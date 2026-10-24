using System.Security.Cryptography;
using System.Text;
using MediatR;
using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Application.Ingest;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Driver;

// The driver app's side of the railway (SPEC-011): my duty today, sign on, the journey, one-tap events, a delay reason, sign off.
// A driver only ever reaches their own duty and the journeys assigned to it; anything else looks like it does not exist.

public sealed record DutyJourneyView(
    Guid JourneyId, string TrainNumber, string Origin, string Destination, string Status, DateTimeOffset DepartsUtc, DateTimeOffset ArrivesUtc,
    string? Platform, int Stops, int? DelayMinutes);

// Status: Active (signed on), Ready (can sign on now), Upcoming (later today), Past (ended or missed).
public sealed record DutyView(
    Guid Id, string Status, string State, DateTimeOffset FromUtc, DateTimeOffset ToUtc, string? UnitCode, bool CanSignOn, bool NeedsReview,
    DateTimeOffset? SignedOnAt, IReadOnlyList<DutyJourneyView> Journeys);

public sealed record MyDutyView(DateOnly Date, IReadOnlyList<DutyView> Duties);

public sealed record DriverStopView(
    int Sequence, string StationCode, string StationName, string Kind, string State, DateTimeOffset? ScheduledUtc, DateTimeOffset? ExpectedUtc,
    DateTimeOffset? ActualUtc, string? Platform, string? OriginalPlatform, bool IsNext);

// Next: the one tap that makes sense now ("arrived" or "departed" at a stop), or null when there is nothing to do.
public sealed record NextEventView(int StopSequence, string StationName, string Type);

public sealed record DriverJourneyView(
    Guid JourneyId, string TrainNumber, string Origin, string Destination, string Lifecycle, int? DelayMinutes, string? DelayReasonCode,
    string DutyState, string? UnitCode, bool CanAct, NextEventView? Next, IReadOnlyList<DriverStopView> Stops);

internal static class DriverViews
{
    public static string Status(DutyData duty, DateTimeOffset now)
    {
        if (duty.State is DutyState.SignedOff or DutyState.Orphaned || now > duty.RosteredTo)
            return "Past";
        if (duty.State is DutyState.SignedOn or DutyState.OnJourney)
            return "Active";

        return now >= duty.RosteredFrom - CrewDuty.SignOnEarly ? "Ready" : "Upcoming";
    }

    // Next: the first journey not finished, Running: under way, Done: finished or cancelled, Later: the rest.
    public static List<DutyJourneyView> Journeys(DutyData duty, IReadOnlyDictionary<string, string> names)
    {
        var next = duty.Journeys.FirstOrDefault(j => j.Lifecycle is JourneyLifecycle.Planned or JourneyLifecycle.Ready)?.JourneyId;
        return duty.Journeys.Select(j => new DutyJourneyView(
            j.JourneyId, j.TrainNumber, names.GetValueOrDefault(j.OriginCode, j.OriginCode), names.GetValueOrDefault(j.DestinationCode, j.DestinationCode),
            j.Lifecycle switch
            {
                JourneyLifecycle.Running or JourneyLifecycle.Interrupted => "Running",
                JourneyLifecycle.Completed or JourneyLifecycle.Cancelled or JourneyLifecycle.TerminatedShort or JourneyLifecycle.TerminatedEmergency => "Done",
                _ => j.JourneyId == next ? "Next" : "Later"
            },
            j.DepartsUtc, j.ArrivesUtc, j.FirstPlatform, j.StopCount, j.DelayMinutes)).ToList();
    }

    public static DutyView Duty(DutyData duty, IReadOnlyDictionary<string, string> names, DateTimeOffset now)
    {
        var status = Status(duty, now);
        return new DutyView(
            duty.DutyId, status, duty.State.ToString(), duty.RosteredFrom, duty.RosteredTo, duty.UnitCode, status == "Ready",
            duty.ReviewState == DutyReviewState.NeedsReview, duty.SignedOnAt, Journeys(duty, names));
    }

    public static async Task<IReadOnlyDictionary<string, string>> NamesAsync(IEnumerable<DutyData> duties, INetworkStations stations, CancellationToken ct) =>
        await stations.GetNamesAsync(duties.SelectMany(d => d.Journeys).SelectMany(j => new[] { j.OriginCode, j.DestinationCode }).Distinct(), null, ct);

    // The origin only departs, the last stop only arrives, every stop in between arrives and then departs. Passing points are skipped.
    public static (int Sequence, string Type)? NextEvent(IReadOnlyList<DriverStopData> stops)
    {
        var ordered = stops.Where(s => s.Kind != JourneyStopKind.Passing).OrderBy(s => s.Sequence).ToList();
        if (ordered.Count == 0)
            return null;

        var first = ordered[0].Sequence;
        var last = ordered[^1].Sequence;
        foreach (var stop in ordered)
        {
            switch (stop.State)
            {
                case JourneyStopState.Pending:
                    return (stop.Sequence, stop.Sequence == first ? "departed" : "arrived");
                case JourneyStopState.Arrived when stop.Sequence != last:
                    return (stop.Sequence, "departed");
                case JourneyStopState.Arrived:
                    return null;
            }
        }

        return null;
    }
}

// ---- My duty ----

public sealed record GetMyDutyQuery : IQuery<MyDutyView>;

public sealed class GetMyDutyQueryHandler(IDriverStore store, INetworkStations stations, INetworkTimeZone timeZone, ICurrentUser user, TimeProvider clock)
    : IQueryHandler<GetMyDutyQuery, MyDutyView>
{
    public async Task<Result<MyDutyView>> Handle(GetMyDutyQuery query, CancellationToken cancellationToken)
    {
        var today = timeZone.Today();
        var duties = await store.ListDutiesAsync(user.UserId ?? string.Empty, today, today, cancellationToken);
        var names = await DriverViews.NamesAsync(duties, stations, cancellationToken);
        var now = clock.GetUtcNow();
        return new MyDutyView(today, duties.Select(d => DriverViews.Duty(d, names, now)).ToList());
    }
}

public sealed record GetDutyHistoryQuery : IQuery<IReadOnlyList<DutyView>>;

public sealed class GetDutyHistoryQueryHandler(IDriverStore store, INetworkStations stations, INetworkTimeZone timeZone, ICurrentUser user, TimeProvider clock)
    : IQueryHandler<GetDutyHistoryQuery, IReadOnlyList<DutyView>>
{
    public async Task<Result<IReadOnlyList<DutyView>>> Handle(GetDutyHistoryQuery query, CancellationToken cancellationToken)
    {
        var today = timeZone.Today();
        var duties = await store.ListDutiesAsync(user.UserId ?? string.Empty, today.AddDays(-14), today.AddDays(-1), cancellationToken);
        var names = await DriverViews.NamesAsync(duties, stations, cancellationToken);
        var now = clock.GetUtcNow();
        return Result.Success<IReadOnlyList<DutyView>>(duties.OrderByDescending(d => d.RosteredFrom).Select(d => DriverViews.Duty(d, names, now)).ToList());
    }
}

// ---- Sign on and off ----

public sealed record SignOnDutyCommand(Guid DutyId) : ICommand;

public sealed class SignOnDutyCommandHandler(IDriverStore store, IJourneysUnitOfWork unitOfWork, IJourneysAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<SignOnDutyCommand>
{
    public async Task<Result> Handle(SignOnDutyCommand command, CancellationToken cancellationToken)
    {
        var me = user.UserId ?? string.Empty;
        var duty = await store.GetDutyAsync(command.DutyId, cancellationToken);
        if (duty is null || duty.DriverSubject != me)
            return JourneysErrors.DutyNotFound(command.DutyId);

        var result = duty.SignOn(clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(me, user.Roles.FirstOrDefault(), "duty.sign_on", "duty", duty.Id.ToString(), null));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record SignOffDutyCommand(Guid DutyId) : ICommand;

public sealed class SignOffDutyCommandHandler(IDriverStore store, IJourneysUnitOfWork unitOfWork, IJourneysAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<SignOffDutyCommand>
{
    public async Task<Result> Handle(SignOffDutyCommand command, CancellationToken cancellationToken)
    {
        var me = user.UserId ?? string.Empty;
        var duty = await store.GetDutyAsync(command.DutyId, cancellationToken);
        if (duty is null || duty.DriverSubject != me)
            return JourneysErrors.DutyNotFound(command.DutyId);

        var result = duty.SignOff(clock.GetUtcNow());
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(me, user.Roles.FirstOrDefault(), "duty.sign_off", "duty", duty.Id.ToString(), null));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- The journey ----

public sealed record GetDriverJourneyQuery(Guid JourneyId) : IQuery<DriverJourneyView>;

public sealed class GetDriverJourneyQueryHandler(IDriverStore store, INetworkStations stations, ICurrentUser user, TimeProvider clock)
    : IQueryHandler<GetDriverJourneyQuery, DriverJourneyView>
{
    public async Task<Result<DriverJourneyView>> Handle(GetDriverJourneyQuery query, CancellationToken cancellationToken)
    {
        var data = await store.GetJourneyAsync(query.JourneyId, user.UserId ?? string.Empty, clock.GetUtcNow(), cancellationToken);
        if (data is null)
            return JourneysErrors.NotYourJourney;

        var names = await stations.GetNamesAsync(data.Stops.Select(s => s.StationCode).Append(data.OriginCode).Append(data.DestinationCode).Distinct(), null, cancellationToken);
        var next = DriverViews.NextEvent(data.Stops);
        var acts = data.DutyState is DutyState.SignedOn or DutyState.OnJourney
            && data.Lifecycle is JourneyLifecycle.Ready or JourneyLifecycle.Running or JourneyLifecycle.Interrupted;

        return new DriverJourneyView(
            data.JourneyId, data.TrainNumber, names.GetValueOrDefault(data.OriginCode, data.OriginCode), names.GetValueOrDefault(data.DestinationCode, data.DestinationCode),
            data.Lifecycle.ToString(), data.DelayMinutes, data.DelayReasonCode, data.DutyState.ToString(), data.UnitCode, acts,
            acts && next is { } n ? new NextEventView(n.Sequence, names.GetValueOrDefault(data.Stops.First(s => s.Sequence == n.Sequence).StationCode, "?"), n.Type) : null,
            data.Stops.Select(s => new DriverStopView(
                s.Sequence, s.StationCode, names.GetValueOrDefault(s.StationCode, s.StationCode), s.Kind.ToString(), s.State.ToString(),
                s.ScheduledArrivalUtc ?? s.ScheduledDepartureUtc, s.ExpectedArrivalUtc ?? s.ExpectedDepartureUtc, s.ActualDepartureUtc ?? s.ActualArrivalUtc,
                s.Platform, s.OriginalPlatform, next is { } nn && nn.Sequence == s.Sequence)).ToList());
    }
}

// ---- One-tap events ----

// Type is arrived, departed or skipped (the train does not stop there). Sequence is the app's own counter: it only goes up, so a message
// sent twice (a retry, or the offline queue replaying) is stored once.
public sealed record RecordDriverEventCommand(Guid JourneyId, int StopSequence, string Type, long Sequence, DateTimeOffset? OccurredAtUtc) : ICommand<IngestItemResult>;

public sealed class RecordDriverEventCommandHandler(IDriverStore store, INetworkStations stations, ISender sender, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<RecordDriverEventCommand, IngestItemResult>
{
    public const int DriverSourceRank = 2;

    public async Task<Result<IngestItemResult>> Handle(RecordDriverEventCommand command, CancellationToken cancellationToken)
    {
        var me = user.UserId ?? string.Empty;
        var now = clock.GetUtcNow();

        var data = await store.GetJourneyAsync(command.JourneyId, me, now, cancellationToken);
        if (data is null)
            return JourneysErrors.NotYourJourney;
        if (data.DutyState is not (DutyState.SignedOn or DutyState.OnJourney))
            return JourneysErrors.DutyNotSignedOn;

        var type = command.Type.Trim().ToLowerInvariant();
        if (type is not ("arrived" or "departed" or "skipped"))
            return Error.Validation("Journeys.EventTypeUnknown", "The event is arrived, departed or skipped.");

        // The driver taps the stop the train is at. Anything else is refused with what was expected (REQ-DRV-011).
        var next = DriverViews.NextEvent(data.Stops);
        if (next is null)
            return JourneysErrors.StopDoesNotMatch("There is nothing left to report on this journey.");

        var wanted = next.Value;
        var expected = data.Stops.First(s => s.Sequence == wanted.Sequence);
        var matches = type == "skipped"
            ? command.StopSequence == wanted.Sequence && wanted.Type == "arrived"
            : command.StopSequence == wanted.Sequence && type == wanted.Type;
        if (!matches)
        {
            var names = await stations.GetNamesAsync([expected.StationCode], null, cancellationToken);
            return JourneysErrors.StopDoesNotMatch($"Next is {wanted.Type} at {names.GetValueOrDefault(expected.StationCode, expected.StationCode)}.");
        }

        var at = command.OccurredAtUtc is { } given && given <= now.AddMinutes(1) ? given : now;
        var result = await sender.Send(
            new IngestStopEventsCommand(DriverSender(me), [new StopEventInput(command.Sequence, command.JourneyId, command.StopSequence, type == "skipped" ? "passed" : type, at)]),
            cancellationToken);
        if (result.IsFailure)
            return result.Error;

        var item = result.Value.Results[0];
        if (item.Disposition is nameof(EventDisposition.Rejected))
            return Error.Conflict("Journeys.EventRejected", $"The event was not accepted ({item.Reason}).");

        return item;
    }

    // The driver is a source of their own: one counter per driver, ranked below the train feed.
    internal static FeedSender DriverSender(string subject) =>
        new(new Guid(MD5.HashData(Encoding.UTF8.GetBytes($"smartrail:driver:{subject}"))), "DRV", DriverSourceRank, EventSourceKind.Driver);
}

// ---- Delay reason ----

public sealed record RecordDelayReasonCommand(Guid JourneyId, string ReasonCode) : ICommand;

public sealed class RecordDelayReasonCommandHandler(
    IDriverStore store, IJourneyRepository journeys, IJourneysUnitOfWork unitOfWork, IJourneysAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<RecordDelayReasonCommand>
{
    public async Task<Result> Handle(RecordDelayReasonCommand command, CancellationToken cancellationToken)
    {
        var me = user.UserId ?? string.Empty;
        var now = clock.GetUtcNow();

        if (await store.GetJourneyAsync(command.JourneyId, me, now, cancellationToken) is not { } data)
            return JourneysErrors.NotYourJourney;
        if (data.DutyState is not (DutyState.SignedOn or DutyState.OnJourney))
            return JourneysErrors.DutyNotSignedOn;

        var journey = await journeys.GetWithStopsAsync(command.JourneyId, cancellationToken);
        if (journey is null)
            return JourneysErrors.JourneyNotFound(command.JourneyId);

        var before = journey.DelayReasonCode;
        var result = journey.RecordDelayReason(command.ReasonCode, now);
        if (result.IsFailure)
            return result;

        audit.Record(new AuditRecord(me, user.Roles.FirstOrDefault(), "journey.delay_reason", "journey", journey.Id.ToString(), command.ReasonCode,
            Before: new { Reason = before }, After: new { Reason = command.ReasonCode, journey.TrainNumber }));
        await unitOfWork.TrySaveAsync(cancellationToken);
        return Result.Success();
    }
}
