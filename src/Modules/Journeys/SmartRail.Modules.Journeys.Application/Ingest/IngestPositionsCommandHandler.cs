using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Ingest;

public sealed class IngestPositionsCommandHandler(
    IJourneyRepository journeys,
    IIngestStore store,
    IJourneysUnitOfWork unitOfWork,
    TimeProvider clock,
    LiveRules rules)
    : ICommandHandler<IngestPositionsCommand, IngestResponse>
{
    private const int Attempts = 3;

    public async Task<Result<IngestResponse>> Handle(IngestPositionsCommand command, CancellationToken cancellationToken)
    {
        var results = new List<IngestItemResult>(command.Readings.Count);
        foreach (var input in command.Readings)
            results.Add(await ProcessAsync(command.Source, input, cancellationToken));

        return new IngestResponse(results);
    }

    private async Task<IngestItemResult> ProcessAsync(FeedSender source, PositionInput input, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            var (result, saved) = await TryProcessAsync(source, input, ct);
            if (saved)
                return result;
        }

        IngestMetrics.Rejected("conflict", source.Code);
        return new IngestItemResult(input.Sequence, nameof(EventDisposition.Rejected), "conflict_try_again");
    }

    private async Task<(IngestItemResult Result, bool Saved)> TryProcessAsync(FeedSender source, PositionInput input, CancellationToken ct)
    {
        var now = clock.GetUtcNow();

        if (await store.SequenceSeenAsync(source.Id, input.Sequence, ct))
        {
            IngestMetrics.Duplicate(source.Code);
            return (new IngestItemResult(input.Sequence, nameof(EventDisposition.Duplicate)), true);
        }

        var highest = await store.HighestAcceptedSequenceAsync(source.Id, ct);
        if (highest is not null && input.Sequence < highest)
            return await RejectAsync(source, input, now, "out_of_order", null, ct);

        if (input.RecordedAt > now.AddSeconds(rules.FutureToleranceSeconds) || (double)input.SpeedKmh > rules.MaxSpeedKmh)
            return await RejectAsync(source, input, now, "implausible", null, ct);

        // The vehicle reaches a journey only through its current assignment (REQ-LIVE-010).
        var journeyId = await store.ResolveJourneyAsync(input.VehicleCode, input.RecordedAt, ct);
        if (journeyId is null)
            return await StoreAsync(source, input, now, null, accepted: true, null, ghost: true, ct);

        var journey = await journeys.GetWithStopsAsync(journeyId.Value, ct);
        if (journey is null)
            return await StoreAsync(source, input, now, null, accepted: true, null, ghost: true, ct);

        var section = SectionIndex(journey, input.FromStation, input.ToStation);
        if (section < 0)
            return await RejectAsync(source, input, now, "implausible", journey.Id, ct);

        var previous = await store.LastAcceptedReadingAsync(input.VehicleCode, journey.Id, ct);
        if (previous is not null)
        {
            var before = SectionIndex(journey, previous.SectionFromStationCode, previous.SectionToStationCode);

            // The train may stay in its section or enter the next one; it cannot skip a section or go back.
            if (before >= 0 && (section < before || section > before + 1))
                return await RejectAsync(source, input, now, "implausible", journey.Id, ct);

            if (before == section && previous.Progress is { } last && input.Progress < last - (decimal)rules.MaxProgressRegression)
                return await RejectAsync(source, input, now, "implausible", journey.Id, ct);
        }

        journey.RecordPosition(now);
        return await StoreAsync(source, input, now, journey.Id, accepted: true, null, ghost: false, ct);
    }

    // The position of the section from -> to among the journey's consecutive stops, or -1 when it is not one of its sections.
    private static int SectionIndex(Journey journey, string? from, string? to)
    {
        var stations = journey.StationCodesInOrder();
        for (var i = 0; i < stations.Count - 1; i++)
        {
            if (stations[i] == from && stations[i + 1] == to)
                return i;
        }

        return -1;
    }

    private Task<(IngestItemResult Result, bool Saved)> RejectAsync(
        FeedSender source, PositionInput input, DateTimeOffset now, string reason, Guid? journeyId, CancellationToken ct) =>
        StoreAsync(source, input, now, journeyId, accepted: false, reason, ghost: false, ct);

    private async Task<(IngestItemResult Result, bool Saved)> StoreAsync(
        FeedSender source,
        PositionInput input,
        DateTimeOffset now,
        Guid? journeyId,
        bool accepted,
        string? rejectReason,
        bool ghost,
        CancellationToken ct)
    {
        store.Add(new TelemetryReading(
            source.Id, input.VehicleCode, input.RecordedAt, now, input.Sequence,
            input.FromStation, input.ToStation, input.Progress, input.SpeedKmh, journeyId, accepted, rejectReason));

        if (!await unitOfWork.TrySaveAsync(ct))
            return (new IngestItemResult(input.Sequence, nameof(EventDisposition.Rejected), "conflict_try_again", journeyId), false);

        if (ghost)
        {
            IngestMetrics.Ghost(source.Code);
            return (new IngestItemResult(input.Sequence, "Ghost", "no_journey"), true);
        }

        if (!accepted)
        {
            IngestMetrics.Rejected(rejectReason!, source.Code);
            return (new IngestItemResult(input.Sequence, nameof(EventDisposition.Rejected), rejectReason, journeyId), true);
        }

        IngestMetrics.Accepted("Position", source.Code);
        return (new IngestItemResult(input.Sequence, nameof(EventDisposition.Applied), null, journeyId), true);
    }
}
