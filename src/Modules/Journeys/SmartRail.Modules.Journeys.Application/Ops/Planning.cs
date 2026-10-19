using SmartRail.Application.Core.Abstractions;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Errors;
using SmartRail.Modules.Journeys.Domain.Fleet;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Ops;

// ---- views ----

public sealed record DriverOption(
    Guid DutyId,
    string DriverSubject,
    string DriverName,
    DateTimeOffset DutyFromUtc,
    DateTimeOffset DutyToUtc,
    bool Eligible,
    string Note,
    IReadOnlyList<PlanningCheck> Checks);

// A check the dialog lists with a tick: Qualified on this route, Rest period met, No overlapping duty, Duty covers the journey.
public sealed record PlanningCheck(string Code, string Label, bool Passed);

public sealed record UnitOption(Guid UnitId, string Code, string Name, bool Available, string Note);

public sealed record AssignmentOptions(
    Guid JourneyId,
    string TrainNumber,
    string Route,
    DateTimeOffset DepartsUtc,
    int Version,
    IReadOnlyList<DriverOption> Drivers,
    IReadOnlyList<UnitOption> Units);

public sealed record PlanningNeed(
    Guid JourneyId,
    string TrainNumber,
    string Route,
    DateTimeOffset DepartsUtc,
    int DepartsInMinutes,
    string Missing,
    string Detail,
    DriverOption? SuggestedDriver,
    UnitOption? SuggestedUnit);

public sealed record UnitAlert(Guid UnitId, string Code, DateTimeOffset From, DateTimeOffset? To, string Reason, int AffectedJourneys);

public sealed record PlanningNeeds(DateOnly ServiceDate, IReadOnlyList<UnitAlert> UnitAlerts, IReadOnlyList<PlanningNeed> Items);

public sealed record UnitAssignmentRow(Guid UnitId, string Code, string Name, IReadOnlyList<string> Drivers, IReadOnlyList<string> JourneyTimes, string Status);

public sealed record UnitRow(Guid UnitId, string Code, string Name, string Kind, string Status, DateTimeOffset? OutOfServiceFromUtc, DateTimeOffset? OutOfServiceToUtc, string? Reason, int JourneysToday);

public sealed record CrewRow(Guid DutyId, string DriverSubject, string DriverName, DateTimeOffset FromUtc, DateTimeOffset ToUtc, string State, IReadOnlyList<string> JourneyTimes);

public sealed record AffectedJourney(
    Guid JourneyId,
    string TrainNumber,
    string Route,
    DateTimeOffset DepartsUtc,
    string? CurrentUnit,
    UnitOption? SuggestedUnit,
    string Status);

public sealed record UnitImpact(
    Guid UnitId,
    string Code,
    Guid? PeriodId,
    DateTimeOffset? OutOfServiceFromUtc,
    string? Reason,
    int Uncovered,
    IReadOnlyList<AffectedJourney> Journeys);

// ---- the rules the suggestions and checks share ----

internal static class PlanningRules
{
    // Minutes between the end of one duty and the start of the next for the same driver.
    public const int MinRestMinutes = 30;

    public static string Route(OpsJourneyData j, IReadOnlyDictionary<string, string> names) =>
        $"{names.GetValueOrDefault(j.OriginStationCode, j.OriginStationCode)} → {names.GetValueOrDefault(j.DestinationStationCode, j.DestinationStationCode)}";

    public static bool Active(OpsJourneyData j) =>
        j.Lifecycle is JourneyLifecycle.Planned or JourneyLifecycle.Ready or JourneyLifecycle.Running or JourneyLifecycle.Interrupted;

    // One driver option for one journey, with the four checks.
    public static DriverOption DriverOption(
        OpsJourneyData journey, OpsDutyData duty, string name, IReadOnlyList<OpsJourneyData> day, INetworkTimeZone tz)
    {
        var mine = day.Where(o => o.DutyId == duty.Id && o.Id != journey.Id && Active(o)).ToList();
        var overlap = mine.FirstOrDefault(o => o.ScheduledDepartureUtc < journey.ScheduledArrivalUtc && journey.ScheduledDepartureUtc < o.ScheduledArrivalUtc);
        var previous = mine.Where(o => o.ScheduledArrivalUtc <= journey.ScheduledDepartureUtc).OrderByDescending(o => o.ScheduledArrivalUtc).FirstOrDefault();
        var restEnds = previous?.ScheduledArrivalUtc.AddMinutes(MinRestMinutes);
        var rested = restEnds is null || restEnds <= journey.ScheduledDepartureUtc;
        var covers = duty.RosteredFrom <= journey.ScheduledDepartureUtc && journey.ScheduledArrivalUtc <= duty.RosteredTo;

        var checks = new[]
        {
            // Route qualification is not modelled in this release, so it always passes.
            new PlanningCheck("qualified", "Qualified on this route", true),
            new PlanningCheck("rested", "Rest period met", rested),
            new PlanningCheck("no_overlap", "No overlapping duty", overlap is null),
            new PlanningCheck("covers", "Duty covers the journey", covers)
        };

        var note = overlap is not null ? $"Overlaps train {overlap.TrainNumber} ({tz.Hm(overlap.ScheduledDepartureUtc)} to {tz.Hm(overlap.ScheduledArrivalUtc)})"
            : !covers ? $"Duty is {tz.Hm(duty.RosteredFrom)} to {tz.Hm(duty.RosteredTo)}"
            : !rested ? $"Rest period ends {tz.Hm(restEnds!.Value)}"
            : "Qualified on this route · rest period met";

        return new DriverOption(duty.Id, duty.DriverSubject, name, duty.RosteredFrom, duty.RosteredTo, checks.All(c => c.Passed), note, checks);
    }

    // One unit option: available means in service and not on another journey during this one.
    public static UnitOption UnitOption(
        OpsJourneyData journey, OpsVehicleData vehicle, IReadOnlyList<OpsUnavailability> unavailable, IReadOnlyList<OpsJourneyData> day)
    {
        var out_ = unavailable.FirstOrDefault(u => u.VehicleId == vehicle.Id && u.From < journey.ScheduledArrivalUtc && journey.ScheduledDepartureUtc < (u.To ?? DateTimeOffset.MaxValue));
        var busy = day.FirstOrDefault(o => o.VehicleId == vehicle.Id && o.Id != journey.Id && Active(o)
            && o.ScheduledDepartureUtc < journey.ScheduledArrivalUtc && journey.ScheduledDepartureUtc < o.ScheduledArrivalUtc);

        var note = !vehicle.IsActive ? "Not in the fleet"
            : out_ is not null ? $"Out of service: {out_.Reason}"
            : busy is not null ? $"On train {busy.TrainNumber} at that time"
            : "Available";

        return new UnitOption(vehicle.Id, vehicle.Code, vehicle.Name, vehicle.IsActive && out_ is null && busy is null, note);
    }
}

// ---- Planning page ----

public sealed record GetPlanningNeedsQuery(DateOnly? Date) : IQuery<PlanningNeeds>;

public sealed class GetPlanningNeedsQueryHandler(
    IOpsJourneyReader reader, INetworkStations stations, IStaffNames staff, INetworkTimeZone timeZone, TimeProvider clock)
    : IQueryHandler<GetPlanningNeedsQuery, PlanningNeeds>
{
    public async Task<Result<PlanningNeeds>> Handle(GetPlanningNeedsQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var date = query.Date ?? timeZone.Today();
        var day = await reader.ListAsync(new OpsJourneyFilter(date, null, null, null), cancellationToken);
        var vehicles = await reader.ListVehiclesAsync(cancellationToken);
        var duties = await reader.ListDutiesAsync(date, cancellationToken);
        var unavailable = await reader.ListUnavailabilityAsync(null, date.ToDateTime(TimeOnly.MinValue).ToUniversalTime().AddHours(-6), now.AddDays(2), cancellationToken);
        var names = await stations.GetNamesAsync(day.SelectMany(j => new[] { j.OriginStationCode, j.DestinationStationCode }).Distinct(), null, cancellationToken);
        var driverNames = await staff.GetNamesAsync(duties.Select(d => d.DriverSubject), cancellationToken);

        var items = new List<PlanningNeed>();
        foreach (var j in day.Where(j => PlanningRules.Active(j) && j.Lifecycle != JourneyLifecycle.Running && j.ScheduledDepartureUtc > now - TimeSpan.FromMinutes(5)))
        {
            var noDriver = j.DutyId is null;
            var noUnit = j.VehicleId is null;
            var unitDown = !noUnit && unavailable.Any(u => u.VehicleId == j.VehicleId && u.From < j.ScheduledArrivalUtc && j.ScheduledDepartureUtc < (u.To ?? DateTimeOffset.MaxValue));

            DriverOption? conflict = null;
            if (!noDriver)
            {
                var duty = duties.FirstOrDefault(d => d.Id == j.DutyId);
                if (duty is not null)
                {
                    var option = PlanningRules.DriverOption(j, duty, driverNames.GetValueOrDefault(duty.DriverSubject, duty.DriverSubject), day, timeZone);
                    if (!option.Eligible)
                        conflict = option;
                }
            }

            if (!noDriver && !noUnit && !unitDown && conflict is null)
                continue;

            var missing = noDriver && noUnit ? "Driver and unit" : noDriver ? "Driver" : noUnit ? "Unit" : unitDown ? "Unit out of service" : "Rest period conflict";
            var detail = conflict is not null ? conflict.Note : unitDown ? "The assigned unit is out of service." : "Nobody is assigned yet.";

            DriverOption? suggestedDriver = null;
            if (noDriver)
            {
                suggestedDriver = duties
                    .Select(d => PlanningRules.DriverOption(j, d, driverNames.GetValueOrDefault(d.DriverSubject, d.DriverSubject), day, timeZone))
                    .Where(o => o.Eligible).OrderBy(o => o.DutyFromUtc).FirstOrDefault();
            }

            UnitOption? suggestedUnit = null;
            if (noUnit || unitDown)
            {
                suggestedUnit = vehicles.Select(v => PlanningRules.UnitOption(j, v, unavailable, day)).Where(o => o.Available).OrderBy(o => o.Code).FirstOrDefault();
            }

            items.Add(new PlanningNeed(j.Id, j.TrainNumber, PlanningRules.Route(j, names), j.ScheduledDepartureUtc,
                (int)Math.Max((j.ScheduledDepartureUtc - now).TotalMinutes, 0), missing, detail, suggestedDriver, suggestedUnit));
        }

        var alerts = new List<UnitAlert>();
        foreach (var u in unavailable.Where(u => u.From <= now.AddHours(1)))
        {
            var vehicle = vehicles.FirstOrDefault(v => v.Id == u.VehicleId);
            var affected = day.Count(j => j.VehicleId == u.VehicleId && PlanningRules.Active(j) && u.From < j.ScheduledArrivalUtc && j.ScheduledDepartureUtc < (u.To ?? DateTimeOffset.MaxValue));
            if (vehicle is not null && affected > 0)
                alerts.Add(new UnitAlert(u.VehicleId, vehicle.Code, u.From, u.To, u.Reason, affected));
        }

        return new PlanningNeeds(date, alerts, items.OrderBy(i => i.DepartsUtc).ToList());
    }
}

// The Assign dialog: every driver and unit that could take this journey, with the checks.
public sealed record GetAssignmentOptionsQuery(Guid JourneyId) : IQuery<AssignmentOptions>;

public sealed class GetAssignmentOptionsQueryHandler(
    IOpsJourneyReader reader, INetworkStations stations, IStaffNames staff, INetworkTimeZone timeZone)
    : IQueryHandler<GetAssignmentOptionsQuery, AssignmentOptions>
{
    public async Task<Result<AssignmentOptions>> Handle(GetAssignmentOptionsQuery query, CancellationToken cancellationToken)
    {
        var j = await reader.GetAsync(query.JourneyId, cancellationToken);
        if (j is null)
            return JourneysErrors.JourneyNotFound(query.JourneyId);

        var day = await reader.ListAsync(new OpsJourneyFilter(j.ServiceDate, null, null, null), cancellationToken);
        var duties = await reader.ListDutiesAsync(j.ServiceDate, cancellationToken);
        var vehicles = await reader.ListVehiclesAsync(cancellationToken);
        var unavailable = await reader.ListUnavailabilityAsync(null, j.ScheduledDepartureUtc, j.ScheduledArrivalUtc, cancellationToken);
        var names = await stations.GetNamesAsync([j.OriginStationCode, j.DestinationStationCode], null, cancellationToken);
        var driverNames = await staff.GetNamesAsync(duties.Select(d => d.DriverSubject), cancellationToken);

        var drivers = duties
            .Select(d => PlanningRules.DriverOption(j, d, driverNames.GetValueOrDefault(d.DriverSubject, d.DriverSubject), day, timeZone))
            .OrderByDescending(o => o.Eligible).ThenBy(o => o.DriverName).ToList();
        var units = vehicles.Select(v => PlanningRules.UnitOption(j, v, unavailable, day)).OrderByDescending(o => o.Available).ThenBy(o => o.Code).ToList();

        return new AssignmentOptions(j.Id, j.TrainNumber, PlanningRules.Route(j, names), j.ScheduledDepartureUtc, j.Version, drivers, units);
    }
}

public sealed record GetPlanningAssignmentsQuery(DateOnly? Date) : IQuery<IReadOnlyList<UnitAssignmentRow>>;

public sealed class GetPlanningAssignmentsQueryHandler(
    IOpsJourneyReader reader, IStaffNames staff, INetworkTimeZone timeZone, TimeProvider clock)
    : IQueryHandler<GetPlanningAssignmentsQuery, IReadOnlyList<UnitAssignmentRow>>
{
    public async Task<Result<IReadOnlyList<UnitAssignmentRow>>> Handle(GetPlanningAssignmentsQuery query, CancellationToken cancellationToken)
    {
        var date = query.Date ?? timeZone.Today();
        var day = await reader.ListAsync(new OpsJourneyFilter(date, null, null, null), cancellationToken);
        var vehicles = await reader.ListVehiclesAsync(cancellationToken);
        var unavailable = await reader.ListUnavailabilityAsync(null, date.ToDateTime(TimeOnly.MinValue).ToUniversalTime().AddHours(-6), clock.GetUtcNow().AddDays(2), cancellationToken);
        var driverNames = await staff.GetNamesAsync(day.Select(j => j.DriverSubject).Where(s => s is not null).Select(s => s!), cancellationToken);

        IReadOnlyList<UnitAssignmentRow> rows = vehicles
            .Select(v =>
            {
                var mine = day.Where(j => j.VehicleId == v.Id && PlanningRules.Active(j)).OrderBy(j => j.ScheduledDepartureUtc).ToList();
                var down = unavailable.Any(u => u.VehicleId == v.Id && mine.Any(j => u.From < j.ScheduledArrivalUtc && j.ScheduledDepartureUtc < (u.To ?? DateTimeOffset.MaxValue)));
                var drivers = mine.Select(j => j.DriverSubject).Where(s => s is not null).Distinct().Select(s => driverNames.GetValueOrDefault(s!, s!)).ToList();
                var status = down ? "Out of service" : mine.Count == 0 ? "Idle" : mine.Any(j => j.DutyId is null) ? "Needs a driver" : "Covered";
                return new UnitAssignmentRow(v.Id, v.Code, v.Name, drivers, mine.Select(j => timeZone.Hm(j.ScheduledDepartureUtc)).ToList(), status);
            })
            .Where(r => r.Status != "Idle" || r.JourneyTimes.Count > 0)
            .OrderBy(r => r.Code).ToList();

        return Result.Success(rows);
    }
}

public sealed record ListUnitsQuery(DateOnly? Date) : IQuery<IReadOnlyList<UnitRow>>;

public sealed class ListUnitsQueryHandler(IOpsJourneyReader reader, INetworkTimeZone timeZone, TimeProvider clock)
    : IQueryHandler<ListUnitsQuery, IReadOnlyList<UnitRow>>
{
    public async Task<Result<IReadOnlyList<UnitRow>>> Handle(ListUnitsQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var date = query.Date ?? timeZone.Today();
        var day = await reader.ListAsync(new OpsJourneyFilter(date, null, null, null), cancellationToken);
        var vehicles = await reader.ListVehiclesAsync(cancellationToken);
        var periods = await reader.ListUnavailabilityAsync(null, now.AddDays(-1), now.AddDays(30), cancellationToken);

        IReadOnlyList<UnitRow> rows = vehicles.Select(v =>
        {
            var open = periods.Where(p => p.VehicleId == v.Id && p.From <= now.AddHours(24) && (p.To is null || p.To > now)).OrderBy(p => p.From).FirstOrDefault();
            var status = !v.IsActive ? "Retired" : open is null ? "Available" : open.From <= now ? "OutOfService" : "OutOfServiceSoon";
            return new UnitRow(v.Id, v.Code, v.Name, v.Kind, status, open?.From, open?.To, open?.Reason, day.Count(j => j.VehicleId == v.Id && PlanningRules.Active(j)));
        }).OrderBy(r => r.Code).ToList();

        return Result.Success(rows);
    }
}

public sealed record ListCrewQuery(DateOnly? Date) : IQuery<IReadOnlyList<CrewRow>>;

public sealed class ListCrewQueryHandler(IOpsJourneyReader reader, IStaffNames staff, INetworkTimeZone timeZone)
    : IQueryHandler<ListCrewQuery, IReadOnlyList<CrewRow>>
{
    public async Task<Result<IReadOnlyList<CrewRow>>> Handle(ListCrewQuery query, CancellationToken cancellationToken)
    {
        var date = query.Date ?? timeZone.Today();
        var duties = await reader.ListDutiesAsync(date, cancellationToken);
        var day = await reader.ListAsync(new OpsJourneyFilter(date, null, null, null), cancellationToken);
        var names = await staff.GetNamesAsync(duties.Select(d => d.DriverSubject), cancellationToken);

        IReadOnlyList<CrewRow> rows = duties
            .Select(d => new CrewRow(d.Id, d.DriverSubject, names.GetValueOrDefault(d.DriverSubject, d.DriverSubject), d.RosteredFrom, d.RosteredTo, d.State,
                day.Where(j => j.DutyId == d.Id && PlanningRules.Active(j)).OrderBy(j => j.ScheduledDepartureUtc).Select(j => timeZone.Hm(j.ScheduledDepartureUtc)).ToList()))
            .OrderBy(r => r.FromUtc).ThenBy(r => r.DriverName).ToList();

        return Result.Success(rows);
    }
}

// The Unit out of service screen: which journeys lose their unit and who could take over.
public sealed record GetUnitImpactQuery(Guid UnitId) : IQuery<UnitImpact>;

public sealed class GetUnitImpactQueryHandler(IOpsJourneyReader reader, INetworkStations stations, INetworkTimeZone timeZone, TimeProvider clock)
    : IQueryHandler<GetUnitImpactQuery, UnitImpact>
{
    public async Task<Result<UnitImpact>> Handle(GetUnitImpactQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var vehicles = await reader.ListVehiclesAsync(cancellationToken);
        var vehicle = vehicles.FirstOrDefault(v => v.Id == query.UnitId);
        if (vehicle is null)
            return JourneysErrors.VehicleNotFound(query.UnitId);

        var periods = await reader.ListUnavailabilityAsync(query.UnitId, now.AddDays(-1), now.AddDays(30), cancellationToken);
        var period = periods.Where(p => p.To is null || p.To > now).OrderBy(p => p.From).FirstOrDefault();
        if (period is null)
            return new UnitImpact(vehicle.Id, vehicle.Code, null, null, null, 0, []);

        var today = timeZone.Today();
        var affected = new List<AffectedJourney>();
        var allUnavailable = await reader.ListUnavailabilityAsync(null, period.From, period.To ?? now.AddDays(2), cancellationToken);

        foreach (var date in new[] { today, today.AddDays(1) })
        {
            var day = await reader.ListAsync(new OpsJourneyFilter(date, null, null, null), cancellationToken);
            var names = await stations.GetNamesAsync(day.SelectMany(j => new[] { j.OriginStationCode, j.DestinationStationCode }).Distinct(), null, cancellationToken);

            foreach (var j in day.Where(j => j.VehicleId == query.UnitId && PlanningRules.Active(j) && j.Lifecycle != JourneyLifecycle.Running
                && period.From < j.ScheduledArrivalUtc && j.ScheduledDepartureUtc < (period.To ?? DateTimeOffset.MaxValue)))
            {
                var suggested = vehicles.Where(v => v.Id != query.UnitId)
                    .Select(v => PlanningRules.UnitOption(j, v, allUnavailable, day)).Where(o => o.Available).OrderBy(o => o.Code).FirstOrDefault();
                affected.Add(new AffectedJourney(j.Id, j.TrainNumber, PlanningRules.Route(j, names), j.ScheduledDepartureUtc, vehicle.Code, suggested,
                    suggested is null ? "Uncovered" : "Suggested"));
            }
        }

        var ordered = affected.OrderBy(a => a.DepartsUtc).ToList();
        return new UnitImpact(vehicle.Id, vehicle.Code, period.Id, period.From, period.Reason, ordered.Count(a => a.SuggestedUnit is null), ordered);
    }
}

// ---- commands ----

public sealed record AssignResourcesCommand(
    Guid JourneyId, Guid? UnitId, Guid? DutyId, string Reason, bool AcceptRestConflict, int? ExpectedVersion) : ICommand<OpsActionResult>;

public sealed class AssignResourcesCommandHandler(
    JourneyActions actions, IOpsJourneyReader reader, IPlanningStore store, INetworkTimeZone timeZone, ICurrentUser user)
    : ICommandHandler<AssignResourcesCommand, OpsActionResult>
{
    public async Task<Result<OpsActionResult>> Handle(AssignResourcesCommand command, CancellationToken cancellationToken)
    {
        if (command.UnitId is null && command.DutyId is null)
            return JourneysErrors.NothingToAssign;
        if (string.IsNullOrWhiteSpace(command.Reason))
            return JourneysErrors.ReasonRequired;

        var j = await reader.GetAsync(command.JourneyId, cancellationToken);
        if (j is null)
            return JourneysErrors.JourneyNotFound(command.JourneyId);

        var day = await reader.ListAsync(new OpsJourneyFilter(j.ServiceDate, null, null, null), cancellationToken);

        if (command.UnitId is { } unitId)
        {
            var vehicle = await store.GetVehicleAsync(unitId, cancellationToken);
            if (vehicle is null || !vehicle.IsActive)
                return JourneysErrors.VehicleNotFound(unitId);

            var unavailable = await reader.ListUnavailabilityAsync(unitId, j.ScheduledDepartureUtc, j.ScheduledArrivalUtc, cancellationToken);
            if (unavailable.Count > 0)
                return JourneysErrors.VehicleUnavailable;

            var spans = await reader.ListVehicleSpansAsync(unitId, j.ScheduledDepartureUtc, j.ScheduledArrivalUtc, j.Id, cancellationToken);
            if (spans.Count > 0)
                return JourneysErrors.VehicleBusy;
        }

        if (command.DutyId is { } dutyId)
        {
            var duty = await store.GetDutyAsync(dutyId, cancellationToken);
            if (duty is null)
                return JourneysErrors.DutyNotFound(dutyId);
            if (!duty.Covers(j.ScheduledDepartureUtc, j.ScheduledArrivalUtc))
                return JourneysErrors.DutyDoesNotCover;

            var option = PlanningRules.DriverOption(j, new OpsDutyData(duty.Id, duty.DriverSubject, duty.ServiceDate, duty.RosteredFrom, duty.RosteredTo, duty.State.ToString()), duty.DriverSubject, day, timeZone);
            if (option.Checks.First(c => c.Code == "no_overlap").Passed == false)
                return JourneysErrors.DriverBusy;
            if (!option.Checks.First(c => c.Code == "rested").Passed && !command.AcceptRestConflict)
                return JourneysErrors.DriverNotRested(PlanningRules.MinRestMinutes);
        }

        var by = user.UserId ?? "unknown";
        return await actions.RunAsync(command.JourneyId, command.ExpectedVersion, "journey.assign", command.Reason,
            (journey, now) =>
            {
                if (command.UnitId is { } u)
                {
                    var r = journey.PutUnit(u, now, by, command.Reason);
                    if (r.IsFailure)
                        return r;
                }

                return command.DutyId is { } d ? journey.PutDriver(d, now, by) : Result.Success();
            },
            new { command.UnitId, command.DutyId, command.AcceptRestConflict }, cancellationToken);
    }
}

public sealed record UnassignResourcesCommand(Guid JourneyId, bool Unit, bool Driver, string Reason, int? ExpectedVersion) : ICommand<OpsActionResult>;

public sealed class UnassignResourcesCommandHandler(JourneyActions actions) : ICommandHandler<UnassignResourcesCommand, OpsActionResult>
{
    public Task<Result<OpsActionResult>> Handle(UnassignResourcesCommand command, CancellationToken cancellationToken)
    {
        if (!command.Unit && !command.Driver)
            return Task.FromResult<Result<OpsActionResult>>(JourneysErrors.NothingToAssign);
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Task.FromResult<Result<OpsActionResult>>(JourneysErrors.ReasonRequired);

        return actions.RunAsync(command.JourneyId, command.ExpectedVersion, "journey.unassign", command.Reason,
            (j, now) =>
            {
                if (command.Unit)
                {
                    var r = j.TakeOffUnit(now);
                    if (r.IsFailure)
                        return r;
                }

                return command.Driver ? j.TakeOffDriver(now) : Result.Success();
            },
            new { command.Unit, command.Driver }, cancellationToken);
    }
}

// A driver's shift for a day. Rostering is the scheduler's job (it is how a driver becomes available to assign).
public sealed record CreateDutyCommand(string DriverSubject, DateTimeOffset FromUtc, DateTimeOffset ToUtc) : ICommand<CrewRow>;

public sealed class CreateDutyCommandHandler(
    IPlanningStore store, IStaffNames staff, INetworkTimeZone timeZone, IJourneysUnitOfWork unitOfWork, IJourneysAuditLog audit, ICurrentUser user)
    : ICommandHandler<CreateDutyCommand, CrewRow>
{
    public async Task<Result<CrewRow>> Handle(CreateDutyCommand command, CancellationToken cancellationToken)
    {
        var names = await staff.GetNamesAsync([command.DriverSubject], cancellationToken);
        if (!names.ContainsKey(command.DriverSubject))
            return JourneysErrors.DriverUnknown;

        var date = timeZone.LocalDate(command.FromUtc);
        var duty = CrewDuty.Create(command.DriverSubject, date, command.FromUtc, command.ToUtc);
        if (duty.IsFailure)
            return duty.Error;

        store.AddDuty(duty.Value);
        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "duty.create", "duty", duty.Value.Id.ToString(), null,
            After: new { command.DriverSubject, command.FromUtc, command.ToUtc }));

        if (!await unitOfWork.TrySaveAsync(cancellationToken))
            return JourneysErrors.AssignmentOverlaps;   // the database refuses overlapping duties for one driver

        return new CrewRow(duty.Value.Id, command.DriverSubject, names[command.DriverSubject], command.FromUtc, command.ToUtc, duty.Value.State.ToString(), []);
    }
}

// A unit that cannot be used (REQ-OPS-012). The answer lists the journeys that lose it and who could take over.
public sealed record MarkUnitOutOfServiceCommand(Guid UnitId, DateTimeOffset FromUtc, DateTimeOffset? ToUtc, string Reason) : ICommand<UnitImpact>;

public sealed class MarkUnitOutOfServiceCommandHandler(
    IPlanningStore store, IJourneysUnitOfWork unitOfWork, IJourneysAuditLog audit, ICurrentUser user, TimeProvider clock, MediatR.ISender sender)
    : ICommandHandler<MarkUnitOutOfServiceCommand, UnitImpact>
{
    public async Task<Result<UnitImpact>> Handle(MarkUnitOutOfServiceCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await store.GetVehicleAsync(command.UnitId, cancellationToken);
        if (vehicle is null)
            return JourneysErrors.VehicleNotFound(command.UnitId);

        var now = clock.GetUtcNow();
        var period = VehicleUnavailability.Create(command.UnitId, command.FromUtc, command.ToUtc, command.Reason, user.UserId ?? "unknown", now);
        if (period.IsFailure)
            return period.Error;

        store.AddUnavailability(period.Value);
        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "unit.out_of_service", "unit", vehicle.Id.ToString(), command.Reason,
            After: new { vehicle.Code, command.FromUtc, command.ToUtc }));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await sender.Send(new GetUnitImpactQuery(command.UnitId), cancellationToken);
    }
}

public sealed record EndUnitOutOfServiceCommand(Guid UnitId, Guid PeriodId) : ICommand;

public sealed class EndUnitOutOfServiceCommandHandler(
    IPlanningStore store, IJourneysUnitOfWork unitOfWork, IJourneysAuditLog audit, ICurrentUser user, TimeProvider clock)
    : ICommandHandler<EndUnitOutOfServiceCommand>
{
    public async Task<Result> Handle(EndUnitOutOfServiceCommand command, CancellationToken cancellationToken)
    {
        var period = await store.GetUnavailabilityAsync(command.PeriodId, cancellationToken);
        if (period is null || period.VehicleId != command.UnitId)
            return JourneysErrors.UnavailabilityNotFound(command.PeriodId);

        var ended = period.End(clock.GetUtcNow());
        if (ended.IsFailure)
            return ended;

        audit.Record(new AuditRecord(user.UserId ?? "unknown", user.Roles.FirstOrDefault(), "unit.back_in_service", "unit", command.UnitId.ToString(), null));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record Reassignment(Guid JourneyId, Guid UnitId);

public sealed record SuggestionOutcome(Guid JourneyId, string TrainNumber, bool Applied, string? Error);

// Apply the suggested replacements (all of them, or the ones listed). Each journey is its own change with its own audit row.
public sealed record ApplyUnitSuggestionsCommand(Guid UnitId, IReadOnlyList<Reassignment>? Only, string Reason) : ICommand<IReadOnlyList<SuggestionOutcome>>;

public sealed class ApplyUnitSuggestionsCommandHandler(MediatR.ISender sender, IOpsJourneyReader reader)
    : ICommandHandler<ApplyUnitSuggestionsCommand, IReadOnlyList<SuggestionOutcome>>
{
    public async Task<Result<IReadOnlyList<SuggestionOutcome>>> Handle(ApplyUnitSuggestionsCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return JourneysErrors.ReasonRequired;

        var plan = command.Only?.ToDictionary(r => r.JourneyId, r => r.UnitId);
        var impact = await sender.Send(new GetUnitImpactQuery(command.UnitId), cancellationToken);
        if (impact.IsFailure)
            return impact.Error;

        var outcomes = new List<SuggestionOutcome>();
        foreach (var item in impact.Value.Journeys)
        {
            Guid? target = plan is null ? item.SuggestedUnit?.UnitId : plan.TryGetValue(item.JourneyId, out var chosen) ? chosen : null;
            if (target is null)
            {
                if (plan is null)
                    outcomes.Add(new SuggestionOutcome(item.JourneyId, item.TrainNumber, false, "No unit is available."));
                continue;
            }

            var done = await sender.Send(new AssignResourcesCommand(item.JourneyId, target, null, command.Reason, false, null), cancellationToken);
            outcomes.Add(new SuggestionOutcome(item.JourneyId, item.TrainNumber, done.IsSuccess, done.IsSuccess ? null : done.Error.Description));
        }

        _ = reader;
        return Result.Success<IReadOnlyList<SuggestionOutcome>>(outcomes);
    }
}
