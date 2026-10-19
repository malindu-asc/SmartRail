using System.Text.Json;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Ingest;

public sealed class IngestStopEventsCommandHandler(
    IJourneyRepository journeys,
    IIngestStore store,
    IJourneysUnitOfWork unitOfWork,
    TimeProvider clock,
    LiveRules rules)
    : ICommandHandler<IngestStopEventsCommand, IngestResponse>
{
    private const int Attempts = 3;

    public async Task<Result<IngestResponse>> Handle(IngestStopEventsCommand command, CancellationToken cancellationToken)
    {
        var results = new List<IngestItemResult>(command.Events.Count);
        foreach (var input in command.Events)
            results.Add(await ProcessAsync(command.Source, input, cancellationToken));

        return new IngestResponse(results);
    }

    // Each message is its own unit of work, so one bad message never undoes the good ones around it.
    private async Task<IngestItemResult> ProcessAsync(FeedSender source, StopEventInput input, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            var (result, saved) = await TryProcessAsync(source, input, ct);
            if (saved)
                return result;
        }

        // Another writer kept changing the journey. Nothing was stored, so the sender may simply send it again.
        IngestMetrics.Rejected("conflict", source.Code);
        return new IngestItemResult(input.Sequence, nameof(EventDisposition.Rejected), "conflict_try_again", input.JourneyId);
    }

    private async Task<(IngestItemResult Result, bool Saved)> TryProcessAsync(FeedSender source, StopEventInput input, CancellationToken ct)
    {
        var now = clock.GetUtcNow();

        var journey = await journeys.GetWithStopsAsync(input.JourneyId, ct);
        if (journey is null)
        {
            // The history table needs a journey to hang the event on, so an unknown id is counted and not stored (decision D6).
            IngestMetrics.Rejected("unknown_journey", source.Code);
            return (new IngestItemResult(input.Sequence, nameof(EventDisposition.Rejected), "unknown_journey"), true);
        }

        if (await store.SequenceSeenAsync(source.Id, input.Sequence, ct))
        {
            IngestMetrics.Duplicate(source.Code);
            return (new IngestItemResult(input.Sequence, nameof(EventDisposition.Duplicate), null, journey.Id), true);
        }

        var type = Enum.Parse<StopEventType>(input.Type, ignoreCase: true);

        var highest = await store.HighestAcceptedSequenceAsync(source.Id, ct);
        if (highest is not null && input.Sequence < highest)
            return await RecordAsync(source, input, type, now, EventDisposition.Rejected, "out_of_order", null, null, ct);

        if (input.OccurredAt > now.AddSeconds(rules.FutureToleranceSeconds))
            return await RecordAsync(source, input, type, now, EventDisposition.Rejected, "implausible", null, null, ct);

        // Source precedence (REQ-LIVE-006): the better-ranked source wins; the loser is kept in the history.
        var winner = await store.FindWinningEventAsync(journey.Id, input.StopSequence, type, ct);
        if (winner is not null && source.Rank == winner.SourceRank)
            return await RecordAsync(source, input, type, now, EventDisposition.Rejected, "already_recorded", null, null, ct);

        if (winner is not null && source.Rank > winner.SourceRank)
            return await RecordAsync(source, input, type, now, EventDisposition.Applied, null, null, winner, ct);

        var applied = journey.ApplyStopEvent(type, input.StopSequence, input.OccurredAt, now, rules);
        if (applied.IsFailure)
        {
            if (applied.Error.Code == JourneysErrors.JourneyNotReady.Code)
                return await RecordAsync(source, input, type, now, EventDisposition.HeldForCorrection, "journey_not_ready", null, null, ct);

            var reason = applied.Error.Code == JourneysErrors.StopNotFound(0).Code ? "stop_not_found" : "journey_ended";
            return await RecordAsync(source, input, type, now, EventDisposition.Rejected, reason, null, null, ct);
        }

        var payload = applied.Value.InferredStops.Count == 0
            ? "{}"
            : JsonSerializer.Serialize(new { inferred = applied.Value.InferredStops });

        // A better-ranked source replaces the value that stood: the old event is superseded by this one.
        return await RecordAsync(source, input, type, now, EventDisposition.Applied, null, payload, null, ct, replaces: winner);
    }

    private async Task<(IngestItemResult Result, bool Saved)> RecordAsync(
        FeedSender source,
        StopEventInput input,
        StopEventType type,
        DateTimeOffset now,
        EventDisposition disposition,
        string? reason,
        string? payload,
        JourneyEvent? supersededBy,
        CancellationToken ct,
        JourneyEvent? replaces = null)
    {
        var journeyEvent = new JourneyEvent(
            input.JourneyId, type, input.StopSequence, EventSourceKind.Feed, source.Id, source.Rank,
            input.OccurredAt, now, input.Sequence, disposition, reason, payload ?? "{}");

        if (supersededBy is not null)
            journeyEvent.SupersedeBy(supersededBy);

        replaces?.SupersedeBy(journeyEvent);
        store.Add(journeyEvent);

        if (!await unitOfWork.TrySaveAsync(ct))
            return (new IngestItemResult(input.Sequence, nameof(EventDisposition.Rejected), "conflict_try_again", input.JourneyId), false);

        var shown = journeyEvent.Disposition;
        if (shown == EventDisposition.Applied)
            IngestMetrics.Accepted(type.ToString(), source.Code);
        else
            IngestMetrics.Rejected(reason ?? IngestMetrics.Name(shown), source.Code);

        return (new IngestItemResult(input.Sequence, shown.ToString(), journeyEvent.DispositionReason, input.JourneyId), true);
    }
}
