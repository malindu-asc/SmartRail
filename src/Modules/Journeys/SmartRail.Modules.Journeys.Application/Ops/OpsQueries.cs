using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Ops;

// ---- Live board (REQ-OPS-002) ----

public sealed record GetOpsBoardQuery(DateOnly? Date, string? RouteCode, string? StationCode, string? Status, bool AttentionOnly, string? Text)
    : IQuery<OpsBoard>;

public sealed class GetOpsBoardQueryHandler(
    IOpsJourneyReader reader, INetworkStations stations, IStaffNames staff, INetworkTimeZone timeZone, TimeProvider clock, LiveRules rules)
    : IQueryHandler<GetOpsBoardQuery, OpsBoard>
{
    public async Task<Result<OpsBoard>> Handle(GetOpsBoardQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var date = query.Date ?? timeZone.Today();

        var journeys = await reader.ListAsync(new OpsJourneyFilter(date, query.RouteCode?.Trim().ToUpperInvariant(), query.StationCode?.Trim().ToUpperInvariant(), query.Text), cancellationToken);
        var (stationNames, driverNames) = await OpsMapper.NamesAsync(journeys, stations, staff, cancellationToken);
        var unavailable = await UnavailableUnitsAsync(reader, now, cancellationToken);

        var rows = journeys.Select(j => OpsMapper.ToRow(j, stationNames, driverNames, unavailable, now, rules)).ToList();
        var total = rows.Count;
        var attention = rows.Count(r => r.NeedsAttention);

        IEnumerable<OpsJourneyRow> filtered = rows;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            filtered = filtered.Where(r => string.Equals(r.StatusCode, query.Status, StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.StatusKind, query.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (query.AttentionOnly)
            filtered = filtered.Where(r => r.NeedsAttention);

        var items = filtered.OrderBy(r => r.ExpectedDepartureUtc).ThenBy(r => r.TrainNumber, StringComparer.Ordinal).ToList();
        return new OpsBoard(timeZone.TimeZoneId, now, date, total, attention, items);
    }

    internal static async Task<IReadOnlySet<Guid>> UnavailableUnitsAsync(IOpsJourneyReader reader, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var open = await reader.ListUnavailabilityAsync(null, now.AddHours(-1), now.AddHours(OpsMapper.CrewHorizon.Hours + 1), cancellationToken);
        return open.Select(u => u.VehicleId).ToHashSet();
    }
}

// ---- One journey (REQ-OPS-002, REQ-OPS-004) ----

public sealed record GetOpsJourneyQuery(Guid JourneyId) : IQuery<OpsJourneyDetail>;

public sealed class GetOpsJourneyQueryHandler(
    IOpsJourneyReader reader, INetworkStations stations, IStaffNames staff, TimeProvider clock, LiveRules rules)
    : IQueryHandler<GetOpsJourneyQuery, OpsJourneyDetail>
{
    public async Task<Result<OpsJourneyDetail>> Handle(GetOpsJourneyQuery query, CancellationToken cancellationToken)
    {
        var j = await reader.GetAsync(query.JourneyId, cancellationToken);
        if (j is null)
            return JourneysErrors.JourneyNotFound(query.JourneyId);

        var now = clock.GetUtcNow();
        var (stationNames, driverNames) = await OpsMapper.NamesAsync([j], stations, staff, cancellationToken);
        var unavailable = await GetOpsBoardQueryHandler.UnavailableUnitsAsync(reader, now, cancellationToken);
        var row = OpsMapper.ToRow(j, stationNames, driverNames, unavailable, now, rules);

        var events = await reader.GetEventsAsync(j.Id, 200, cancellationToken);
        var duties = j.DutyId is null ? [] : await reader.ListDutiesAsync(j.ServiceDate, cancellationToken);
        var duty = duties.FirstOrDefault(d => d.Id == j.DutyId);
        var vehicle = j.VehicleId is null ? null : (await reader.ListVehiclesAsync(cancellationToken)).FirstOrDefault(v => v.Id == j.VehicleId);

        var live = LiveStatus.Evaluate(j.Lifecycle, j.Confidence, j.LastAcceptedUpdateAt, now, rules);
        var stops = j.Stops.OrderBy(s => s.Sequence).Select(s => ToStop(j, s, events, stationNames, live)).ToList();

        var last = events.Where(e => e.SourceKind == EventSourceKind.Driver && e.Disposition == EventDisposition.Applied)
            .OrderByDescending(e => e.OccurredAtSource).FirstOrDefault();
        var lastText = last is null ? null : $"{Verb(last.Type)} {stationNames.GetValueOrDefault(Code(j, last.StopSequence), Code(j, last.StopSequence))}";

        return new OpsJourneyDetail(
            row, stops,
            new OpsCrew(j.DriverSubject, row.DriverName, duty?.RosteredFrom, duty?.RosteredTo, j.VehicleId, j.VehicleCode, vehicle?.Name, lastText),
            Actions(j, now), j.CancelReasonCode, j.EndReasonCode, j.ControllerNote, j.EndedAt);
    }

    private static OpsStopView ToStop(
        OpsJourneyData j, OpsStopData s, IReadOnlyList<OpsEventData> events, IReadOnlyDictionary<string, string> names, LiveStatus live)
    {
        var done = s.State is JourneyStopState.Departed or JourneyStopState.Arrived;
        var next = j.Lifecycle == JourneyLifecycle.Running
            && s.State == JourneyStopState.Pending && s.Kind != JourneyStopKind.Passing
            && j.Stops.Where(o => o.State == JourneyStopState.Pending && o.Kind != JourneyStopKind.Passing).Min(o => o.Sequence) == s.Sequence;

        var state = s.State switch
        {
            JourneyStopState.Cancelled => "Cancelled",
            JourneyStopState.Skipped => "Skipped",
            JourneyStopState.Departed => "Done",
            JourneyStopState.Arrived => "At stop",
            _ => next ? "Next" : "Later"
        };

        // Where the time came from: the newest applied event at this stop, else our estimate, else the timetable.
        var applied = events.Where(e => e.StopSequence == s.Sequence && e.Disposition == EventDisposition.Applied)
            .OrderByDescending(e => e.ReceivedAt).FirstOrDefault();
        var source = applied is not null
            ? applied.SourceKind switch { EventSourceKind.Feed => "Train feed", EventSourceKind.Driver => "Driver", EventSourceKind.Controller => "Controller", _ => "System" }
            : done ? "Train feed"
            : live.ShowLiveTimes && s.EstimatedDepartureUtc is not null ? "Estimate" : "Scheduled";

        return new OpsStopView(
            s.Sequence, s.StationCode, names.GetValueOrDefault(s.StationCode, s.StationCode), s.Kind.ToString(),
            s.ScheduledArrivalUtc, s.ScheduledDepartureUtc, s.ActualArrivalUtc, s.ActualDepartureUtc,
            live.ShowLiveTimes ? s.EstimatedArrivalUtc : null, live.ShowLiveTimes ? s.EstimatedDepartureUtc : null,
            s.PlatformCode, s.PlatformState == PlatformState.Changed ? s.PlatformOriginalCode : null, s.PlatformState.ToString(), state, source);
    }

    private static string Code(OpsJourneyData j, int sequence) => j.Stops.FirstOrDefault(s => s.Sequence == sequence)?.StationCode ?? string.Empty;

    private static string Verb(StopEventType type) => type switch { StopEventType.Arrived => "Arrived", StopEventType.Departed => "Departed", _ => "Passed" };

    // What the journey's state allows. The user also needs the permission named on each action.
    internal static IReadOnlyList<OpsAction> Actions(OpsJourneyData j, DateTimeOffset now)
    {
        var l = j.Lifecycle;
        var ended = JourneyLifecycleRules.IsFinal(l);

        OpsAction Make(string code, bool allowed, string whyNot, string permission) => new(code, allowed, allowed ? null : whyNot, permission);

        return
        [
            Make("cancel", l is JourneyLifecycle.Planned or JourneyLifecycle.Ready or JourneyLifecycle.Interrupted,
                l == JourneyLifecycle.Running ? "A running train is interrupted or terminated short, not cancelled." : $"A {l} journey cannot be cancelled.", "journey.cancel"),
            Make("terminate_short", l is JourneyLifecycle.Running or JourneyLifecycle.Interrupted, "Only a train that is running can be terminated short.", "journey.terminate_short"),
            Make("interrupt", l == JourneyLifecycle.Running, "Only a running train can be interrupted.", "journey.interrupt"),
            Make("resume", l == JourneyLifecycle.Interrupted, "Only an interrupted train can resume.", "journey.interrupt"),
            Make("reinstate", l == JourneyLifecycle.Cancelled && now < j.ScheduledDepartureUtc,
                l == JourneyLifecycle.Cancelled ? "A cancelled journey can be reinstated only before its scheduled departure." : "Only a cancelled journey can be reinstated.", "journey.reinstate"),
            Make("change_platform", !ended && l != JourneyLifecycle.Cancelled, "The journey is over or cancelled.", "journey.platform.change"),
            Make("emergency_terminate", !ended, "The journey is already over.", "journey.terminate_emergency"),
            Make("assign", !ended && l != JourneyLifecycle.Cancelled, "The journey is over or cancelled.", "journey.driver.assign"),
            Make("message_driver", j.DutyId is not null && !ended, "The journey has no driver, or it is over.", "message.send_to_driver")
        ];
    }
}

// ---- History of one journey ----

public sealed record GetOpsJourneyEventsQuery(Guid JourneyId, int Take) : IQuery<IReadOnlyList<OpsEventView>>;

public sealed class GetOpsJourneyEventsQueryHandler(IOpsJourneyReader reader, INetworkStations stations)
    : IQueryHandler<GetOpsJourneyEventsQuery, IReadOnlyList<OpsEventView>>
{
    public async Task<Result<IReadOnlyList<OpsEventView>>> Handle(GetOpsJourneyEventsQuery query, CancellationToken cancellationToken)
    {
        var j = await reader.GetAsync(query.JourneyId, cancellationToken);
        if (j is null)
            return JourneysErrors.JourneyNotFound(query.JourneyId);

        var events = await reader.GetEventsAsync(j.Id, Math.Clamp(query.Take <= 0 ? 50 : query.Take, 1, 200), cancellationToken);
        var names = await stations.GetNamesAsync(j.Stops.Select(s => s.StationCode), null, cancellationToken);

        IReadOnlyList<OpsEventView> list = events.OrderByDescending(e => e.OccurredAtSource).Select(e =>
        {
            var code = j.Stops.FirstOrDefault(s => s.Sequence == e.StopSequence)?.StationCode ?? string.Empty;
            var where = names.GetValueOrDefault(code, code);
            var verb = e.Type switch { StopEventType.Arrived => "Arrived", StopEventType.Departed => "Departed", _ => "Passed" };
            var source = e.SourceKind switch { EventSourceKind.Feed => "Train feed", EventSourceKind.Driver => "Driver", EventSourceKind.Controller => "Controller", _ => "System" };
            var note = e.Disposition == EventDisposition.Applied ? null : e.DispositionReason ?? e.Disposition.ToString();
            return new OpsEventView(e.Id, e.OccurredAtSource, e.ReceivedAt, e.StopSequence, code, $"{verb} {where}", source, e.Disposition.ToString(), note);
        }).ToList();

        return Result.Success(list);
    }
}

// ---- Reason codes for the dialogs ----

public sealed record GetReasonCodesQuery : IQuery<ReasonCatalogView>;

public sealed class GetReasonCodesQueryHandler : IQueryHandler<GetReasonCodesQuery, ReasonCatalogView>
{
    public Task<Result<ReasonCatalogView>> Handle(GetReasonCodesQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new ReasonCatalogView(ReasonCatalog.Cancel, ReasonCatalog.TerminateShort, ReasonCatalog.Interrupt, ReasonCatalog.Platform)));
}
