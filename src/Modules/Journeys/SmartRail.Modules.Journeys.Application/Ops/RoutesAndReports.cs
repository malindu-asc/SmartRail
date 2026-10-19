using System.Globalization;
using System.Text;
using MediatR;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Ops;

// ---- Routes (the Routes screens) ----

public sealed record OpsRouteSummary(
    string Key,
    string RouteCode,
    string Name,
    string Status,
    string StatusKind,
    int RunningNow,
    int OnTimeRunning,
    bool DataUnknown,
    int Unassigned,
    string? Note,
    int JourneysToday);

public sealed record OpsRouteJourney(
    Guid JourneyId,
    string TrainNumber,
    DateTimeOffset DepartsUtc,
    string Route,
    string? DriverSubject,
    string? DriverName,
    string? UnitCode,
    string Assignment,
    string AssignmentKind);

public sealed record OpsRouteDetail(OpsRouteSummary Summary, int NeedAttention, string? AttentionText, IReadOnlyList<OpsRouteJourney> Journeys);

internal static class RouteMath
{
    // A route here is a direction: where the train starts and where it ends ("Central → Riverside").
    public static string Key(OpsJourneyData j) => $"{j.OriginStationCode}-{j.DestinationStationCode}";

    // How well a journey is staffed: Assigned, Needs a driver, Needs a unit, Needs a driver and unit, Unit out of service,
    // Rest period conflict.
    public static (string Text, string Kind) Assignment(
        OpsJourneyData j, IReadOnlyList<OpsJourneyData> day, IReadOnlyDictionary<Guid, OpsDutyData> duties, IReadOnlyList<OpsUnavailability> unavailable, INetworkTimeZone tz)
    {
        if (j.DutyId is null && j.VehicleId is null)
            return ("Needs a driver and unit", "Warning");
        if (j.DutyId is null)
            return ("Needs a driver", "Warning");
        if (j.VehicleId is null)
            return ("Needs a unit", "Warning");

        if (unavailable.Any(u => u.VehicleId == j.VehicleId && u.From < j.ScheduledArrivalUtc && j.ScheduledDepartureUtc < (u.To ?? DateTimeOffset.MaxValue)))
            return ("Unit out of service", "Critical");

        if (duties.TryGetValue(j.DutyId.Value, out var duty))
        {
            var option = PlanningRules.DriverOption(j, duty, duty.DriverSubject, day, tz);
            if (!option.Checks.First(c => c.Code == "rested").Passed)
                return ("Rest period conflict", "Warning");
        }

        return ("Assigned", "Good");
    }

    public static async Task<(IReadOnlyList<OpsJourneyData> Day, IReadOnlyDictionary<string, string> Names, IReadOnlyDictionary<string, string> Drivers,
        IReadOnlyDictionary<Guid, OpsDutyData> Duties, IReadOnlyList<OpsUnavailability> Unavailable)> LoadAsync(
        IOpsJourneyReader reader, INetworkStations stations, IStaffNames staff, DateOnly date, TimeProvider clock, CancellationToken cancellationToken)
    {
        var day = await reader.ListAsync(new OpsJourneyFilter(date, null, null, null), cancellationToken);
        var (names, drivers) = await OpsMapper.NamesAsync(day, stations, staff, cancellationToken);
        var duties = (await reader.ListDutiesAsync(date, cancellationToken)).ToDictionary(d => d.Id);
        var unavailable = await reader.ListUnavailabilityAsync(null, clock.GetUtcNow().AddDays(-1), clock.GetUtcNow().AddDays(2), cancellationToken);
        return (day, names, drivers, duties, unavailable);
    }

    public static OpsRouteSummary Summarise(
        string key, IReadOnlyList<OpsJourneyData> journeys, IReadOnlyDictionary<string, string> names,
        IReadOnlyList<OpsJourneyData> day, IReadOnlyDictionary<Guid, OpsDutyData> duties, IReadOnlyList<OpsUnavailability> unavailable,
        DateTimeOffset now, LiveRules rules, INetworkTimeZone tz)
    {
        var first = journeys[0];
        var running = journeys.Where(j => j.Lifecycle == JourneyLifecycle.Running).ToList();
        var states = running.Select(j => LiveStatus.Evaluate(j.Lifecycle, j.Confidence, j.LastAcceptedUpdateAt, now, rules)).ToList();
        var allUnknown = running.Count > 0 && states.All(s => s.Confidence == JourneyConfidence.Unknown);
        var onTime = running.Where((j, i) => states[i].Confidence != JourneyConfidence.Unknown && (j.DelayMinutes ?? 0) < 5).Count();

        var soon = journeys.Where(j => PlanningRules.Active(j) && j.Lifecycle != JourneyLifecycle.Running && j.ScheduledDepartureUtc - now <= OpsMapper.CrewHorizon && j.ScheduledDepartureUtc > now - TimeSpan.FromMinutes(5)).ToList();
        var unassigned = soon.Count(j => j.DutyId is null || j.VehicleId is null);
        var unitDown = journeys.FirstOrDefault(j => PlanningRules.Active(j) && j.VehicleId is not null
            && unavailable.Any(u => u.VehicleId == j.VehicleId && u.From < j.ScheduledArrivalUtc && j.ScheduledDepartureUtc < (u.To ?? DateTimeOffset.MaxValue)));
        var broken = journeys.Any(j => j.Lifecycle is JourneyLifecycle.Interrupted or JourneyLifecycle.TerminatedEmergency);
        var delayed = running.Any((j) => (j.DelayMinutes ?? 0) >= 5);

        var (status, kind) = allUnknown ? ("No live data", "Stale")
            : broken || unitDown is not null ? ("Disrupted", "Critical")
            : delayed ? ("Delayed", "Warning")
            : running.Count > 0 ? ("Running", "Good")
            : ("Scheduled", "Neutral");

        string? note = unitDown is not null ? $"Unit {unitDown.VehicleCode} out of service"
            : allUnknown ? $"No data since {tz.Hm(running.Min(j => j.LastAcceptedUpdateAt) ?? now)}"
            : null;

        var name = $"{names.GetValueOrDefault(first.OriginStationCode, first.OriginStationCode)} → {names.GetValueOrDefault(first.DestinationStationCode, first.DestinationStationCode)}";
        return new OpsRouteSummary(key, first.RouteCode, name, status, kind, running.Count, onTime, allUnknown, unassigned, note, journeys.Count);
    }
}

public sealed record GetOpsRoutesQuery(DateOnly? Date) : IQuery<IReadOnlyList<OpsRouteSummary>>;

public sealed class GetOpsRoutesQueryHandler(
    IOpsJourneyReader reader, INetworkStations stations, IStaffNames staff, INetworkTimeZone timeZone, TimeProvider clock, LiveRules rules)
    : IQueryHandler<GetOpsRoutesQuery, IReadOnlyList<OpsRouteSummary>>
{
    public async Task<Result<IReadOnlyList<OpsRouteSummary>>> Handle(GetOpsRoutesQuery query, CancellationToken cancellationToken)
    {
        var (day, names, _, duties, unavailable) = await RouteMath.LoadAsync(reader, stations, staff, query.Date ?? timeZone.Today(), clock, cancellationToken);
        var now = clock.GetUtcNow();

        IReadOnlyList<OpsRouteSummary> summaries = day.GroupBy(RouteMath.Key)
            .Select(g => RouteMath.Summarise(g.Key, g.ToList(), names, day, duties, unavailable, now, rules, timeZone))
            .OrderByDescending(s => s.StatusKind is "Critical" or "Stale" or "Warning").ThenBy(s => s.Name).ToList();

        return Result.Success(summaries);
    }
}

public sealed record GetOpsRouteQuery(string Key, DateOnly? Date) : IQuery<OpsRouteDetail>;

public sealed class GetOpsRouteQueryHandler(
    IOpsJourneyReader reader, INetworkStations stations, IStaffNames staff, INetworkTimeZone timeZone, TimeProvider clock, LiveRules rules)
    : IQueryHandler<GetOpsRouteQuery, OpsRouteDetail>
{
    public async Task<Result<OpsRouteDetail>> Handle(GetOpsRouteQuery query, CancellationToken cancellationToken)
    {
        var (day, names, drivers, duties, unavailable) = await RouteMath.LoadAsync(reader, stations, staff, query.Date ?? timeZone.Today(), clock, cancellationToken);
        var key = query.Key.Trim().ToUpperInvariant();
        var journeys = day.Where(j => RouteMath.Key(j) == key).ToList();
        if (journeys.Count == 0)
            return Error.NotFound("Journeys.RouteNotFound", $"No journeys run from {key} on that day.");

        var summary = RouteMath.Summarise(key, journeys, names, day, duties, unavailable, clock.GetUtcNow(), rules, timeZone);
        var rows = journeys.OrderBy(j => j.ScheduledDepartureUtc).Select(j =>
        {
            var (text, kind) = RouteMath.Assignment(j, day, duties, unavailable, timeZone);
            return new OpsRouteJourney(j.Id, j.TrainNumber, j.ScheduledDepartureUtc, PlanningRules.Route(j, names), j.DriverSubject,
                j.DriverSubject is null ? null : drivers.GetValueOrDefault(j.DriverSubject), j.VehicleCode, text, kind);
        }).ToList();

        var problems = rows.Where(r => r.AssignmentKind != "Good").ToList();
        string? text2 = null;
        if (problems.Count > 0)
        {
            var parts = new List<string>();
            var noDriver = problems.Count(p => p.Assignment.Contains("driver"));
            var noUnit = problems.Count(p => p.Assignment.Contains("unit") && !p.Assignment.Contains("out of service"));
            var rest = problems.Count(p => p.Assignment == "Rest period conflict");
            var down = problems.Count(p => p.Assignment == "Unit out of service");
            if (noDriver > 0) parts.Add($"{noDriver} {(noDriver == 1 ? "journey has" : "journeys have")} no driver");
            if (noUnit > 0) parts.Add($"{noUnit} {(noUnit == 1 ? "has" : "have")} no unit");
            if (down > 0) parts.Add($"{down} {(down == 1 ? "has" : "have")} a unit out of service");
            if (rest > 0) parts.Add($"{rest} {(rest == 1 ? "has" : "have")} a rest period conflict");
            text2 = string.Join(", ", parts) + ". Resolve these before the journeys depart.";
        }

        return new OpsRouteDetail(summary, problems.Count, text2, rows);
    }
}

// ---- Punctuality report (REQ-OPS-014) ----

public sealed record PunctualityDay(DateOnly Date, int Journeys, int? OnTimePercent);

public sealed record PunctualityLine(string RouteCode, string Name, int Journeys, int? OnTimePercent, double AverageDelayMinutes, int Cancelled);

public sealed record PunctualityReport(
    DateOnly From,
    DateOnly To,
    int Journeys,
    int? OnTimePercent,
    double AverageDelayMinutes,
    int Cancelled,
    int CorrectedRecords,
    IReadOnlyList<PunctualityDay> Days,
    IReadOnlyList<PunctualityLine> Lines);

internal static class PunctualityMath
{
    // On time means within 5 minutes of the timetable at the end (state-models.md 1b). Cancelled and ended-early journeys are not on time.
    public static bool OnTime(OpsPunctualityRow r) => r.Lifecycle == JourneyLifecycle.Completed && (r.DelayMinutes ?? 0) < 5;

    public static bool Late(OpsPunctualityRow r) => r.Lifecycle == JourneyLifecycle.Completed && (r.DelayMinutes ?? 0) >= 5;

    public static int? Percent(IReadOnlyCollection<OpsPunctualityRow> rows) =>
        rows.Count == 0 ? null : (int)Math.Round(100.0 * rows.Count(OnTime) / rows.Count, MidpointRounding.AwayFromZero);

    public static double AverageDelay(IReadOnlyCollection<OpsPunctualityRow> rows)
    {
        var late = rows.Where(Late).ToList();
        return late.Count == 0 ? 0 : Math.Round(late.Average(r => r.DelayMinutes ?? 0), 1);
    }
}

public sealed record GetPunctualityReportQuery(DateOnly? From, DateOnly? To, string? RouteCode) : IQuery<PunctualityReport>;

public sealed class GetPunctualityReportQueryHandler(
    IOpsJourneyReader reader, INetworkStations stations, INetworkTimeZone timeZone)
    : IQueryHandler<GetPunctualityReportQuery, PunctualityReport>
{
    public async Task<Result<PunctualityReport>> Handle(GetPunctualityReportQuery query, CancellationToken cancellationToken)
    {
        var to = query.To ?? timeZone.Today();
        var from = query.From ?? to.AddDays(-6);
        if (from > to || to.DayNumber - from.DayNumber > 92)
            return Error.Validation("Journeys.ReportPeriodInvalid", "Choose a period of at most 92 days, ending on or after its start.");

        var route = string.IsNullOrWhiteSpace(query.RouteCode) ? null : query.RouteCode.Trim().ToUpperInvariant();
        var rows = await reader.ListPunctualityAsync(from, to, route, cancellationToken);
        var corrected = await reader.CountCorrectedEventsAsync(timeZone.ToUtc(from, 0), timeZone.ToUtc(to.AddDays(1), 0), cancellationToken);

        var days = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(i => from.AddDays(i))
            .Select(d => { var day = rows.Where(r => r.ServiceDate == d).ToList(); return new PunctualityDay(d, day.Count, PunctualityMath.Percent(day)); }).ToList();

        var lines = new List<PunctualityLine>();
        foreach (var g in rows.GroupBy(r => r.RouteCode).OrderBy(g => g.Key))
        {
            var list = g.ToList();
            lines.Add(new PunctualityLine(g.Key, $"Route {g.Key}", list.Count, PunctualityMath.Percent(list), PunctualityMath.AverageDelay(list), list.Count(r => r.Lifecycle == JourneyLifecycle.Cancelled)));
        }

        _ = stations;
        return new PunctualityReport(from, to, rows.Count, PunctualityMath.Percent(rows), PunctualityMath.AverageDelay(rows),
            rows.Count(r => r.Lifecycle == JourneyLifecycle.Cancelled), corrected, days, lines);
    }
}

// The same report as a CSV file (REQ-OPS-014): one row per route and day.
public sealed record ExportPunctualityCsvQuery(DateOnly? From, DateOnly? To, string? RouteCode) : IQuery<CsvFile>;

public sealed record CsvFile(string FileName, string Content);

public sealed class ExportPunctualityCsvQueryHandler(IOpsJourneyReader reader, INetworkTimeZone timeZone) : IQueryHandler<ExportPunctualityCsvQuery, CsvFile>
{
    public async Task<Result<CsvFile>> Handle(ExportPunctualityCsvQuery query, CancellationToken cancellationToken)
    {
        var to = query.To ?? timeZone.Today();
        var from = query.From ?? to.AddDays(-6);
        if (from > to || to.DayNumber - from.DayNumber > 92)
            return Error.Validation("Journeys.ReportPeriodInvalid", "Choose a period of at most 92 days, ending on or after its start.");

        var route = string.IsNullOrWhiteSpace(query.RouteCode) ? null : query.RouteCode.Trim().ToUpperInvariant();
        var rows = await reader.ListPunctualityAsync(from, to, route, cancellationToken);

        var csv = new StringBuilder("date,route,journeys,on_time,late,cancelled,ended_early,on_time_percent,average_delay_minutes\n");
        foreach (var g in rows.GroupBy(r => (r.ServiceDate, r.RouteCode)).OrderBy(g => g.Key.ServiceDate).ThenBy(g => g.Key.RouteCode))
        {
            var list = g.ToList();
            csv.Append(string.Create(CultureInfo.InvariantCulture,
                $"{g.Key.ServiceDate:yyyy-MM-dd},{g.Key.RouteCode},{list.Count},{list.Count(PunctualityMath.OnTime)},{list.Count(PunctualityMath.Late)},{list.Count(r => r.Lifecycle == JourneyLifecycle.Cancelled)},{list.Count(r => r.Lifecycle is JourneyLifecycle.TerminatedShort or JourneyLifecycle.TerminatedEmergency)},{PunctualityMath.Percent(list)},{PunctualityMath.AverageDelay(list)}\n"));
        }

        return new CsvFile($"punctuality-{from:yyyyMMdd}-{to:yyyyMMdd}.csv", csv.ToString());
    }
}

// ---- what the dashboards and incidents use (Journeys.Contracts) ----

public sealed class OpsJourneyStats(
    IOpsJourneyReader reader, INetworkTimeZone timeZone, TimeProvider clock, LiveRules rules, INetworkStations stations, IStaffNames staff)
    : IOpsJourneyStats
{
    public async Task<OpsStats> GetAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var date = timeZone.Today();
        var day = await reader.ListAsync(new OpsJourneyFilter(date, null, null, null), cancellationToken);

        var running = day.Where(j => j.Lifecycle == JourneyLifecycle.Running).ToList();
        var known = day.Where(j => j.Lifecycle is JourneyLifecycle.Running or JourneyLifecycle.Completed && j.DelayMinutes is not null).ToList();
        int? onTime = known.Count == 0 ? null : (int)Math.Round(100.0 * known.Count(j => j.DelayMinutes < 5) / known.Count, MidpointRounding.AwayFromZero);
        var noData = running.Count(j => LiveStatus.Evaluate(j.Lifecycle, j.Confidence, j.LastAcceptedUpdateAt, now, rules).Confidence == JourneyConfidence.Unknown);

        return new OpsStats(
            date, now, running.Count, onTime,
            running.Count(j => (j.DelayMinutes ?? 0) >= 5), running.Count(j => (j.DelayMinutes ?? 0) >= 15),
            day.Count(j => j.Lifecycle == JourneyLifecycle.Cancelled), noData, day.Count(j => j.Lifecycle == JourneyLifecycle.Completed), day.Count);
    }

    public async Task<IReadOnlyList<OpsAttentionItem>> GetAttentionAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var date = timeZone.Today();
        var day = await reader.ListAsync(new OpsJourneyFilter(date, null, null, null), cancellationToken);
        var (names, _) = await OpsMapper.NamesAsync(day, stations, staff, cancellationToken);
        var unavailable = await reader.ListUnavailabilityAsync(null, now.AddHours(-1), now.AddHours(OpsMapper.CrewHorizon.Hours + 1), cancellationToken);

        var items = new List<OpsAttentionItem>();
        foreach (var j in day)
        {
            var label = $"{timeZone.Hm(j.ScheduledDepartureUtc)} {names.GetValueOrDefault(j.OriginStationCode, j.OriginStationCode)}";

            if (j.Lifecycle is JourneyLifecycle.Running or JourneyLifecycle.Interrupted)
            {
                var live = LiveStatus.Evaluate(j.Lifecycle, j.Confidence, j.LastAcceptedUpdateAt, now, rules);
                if (live.Confidence == JourneyConfidence.Unknown)
                    items.Add(new OpsAttentionItem("NoLiveData", j.Id, "No live data", $"{label} · since {timeZone.Hm(j.LastAcceptedUpdateAt ?? j.ScheduledDepartureUtc)}", "Open", 2));
                if (j.Punctuality == PunctualityBand.MajorDelay)
                    items.Add(new OpsAttentionItem("MajorDelay", j.Id, "Major delay", $"{label} · +{j.DelayMinutes} min", "Open", 2));
                if (j.Lifecycle == JourneyLifecycle.Interrupted)
                    items.Add(new OpsAttentionItem("Interrupted", j.Id, "Journey interrupted", label, "Open", 3));
            }

            var upcoming = j.Lifecycle is JourneyLifecycle.Planned or JourneyLifecycle.Ready && j.ScheduledDepartureUtc > now && j.ScheduledDepartureUtc - now <= OpsMapper.CrewHorizon;
            if (!upcoming)
                continue;

            var departs = j.ScheduledDepartureUtc - now;
            var inText = departs.TotalMinutes >= 90 ? $"{(int)Math.Round(departs.TotalHours)} h" : $"{(int)departs.TotalMinutes} min";
            if (j.DutyId is null)
                items.Add(new OpsAttentionItem("NoDriver", j.Id, "Journey has no driver", $"{label} · departs in {inText}", "Assign", 1));
            if (j.VehicleId is null)
                items.Add(new OpsAttentionItem("NoUnit", j.Id, "Journey has no unit", $"{label} · departs in {inText}", "Assign", 1));
            else if (unavailable.Any(u => u.VehicleId == j.VehicleId && u.From < j.ScheduledArrivalUtc && j.ScheduledDepartureUtc < (u.To ?? DateTimeOffset.MaxValue)))
                items.Add(new OpsAttentionItem("UnitOutOfService", j.Id, "Unit out of service", $"{label} · {j.VehicleCode}", "Assign", 1));
        }

        return items.OrderBy(i => i.Severity).ThenBy(i => i.Detail).ToList();
    }

    public async Task<OpsJourneyFacts?> GetFactsAsync(Guid journeyId, CancellationToken cancellationToken)
    {
        var j = await reader.GetAsync(journeyId, cancellationToken);
        if (j is null)
            return null;

        var next = j.Stops.OrderBy(s => s.Sequence).FirstOrDefault(s => s.State == JourneyStopState.Pending && s.Kind != JourneyStopKind.Passing);
        return new OpsJourneyFacts(j.Id, j.TrainNumber, j.RouteCode, j.Lifecycle.ToString(), j.ScheduledDepartureUtc,
            j.Stops.OrderBy(s => s.Sequence).Select(s => s.StationCode).ToList(), j.DriverSubject, j.VehicleCode, next?.StationCode);
    }

    public async Task<int> CountAffectedAsync(IReadOnlyCollection<string> stationCodes, int hours, CancellationToken cancellationToken) =>
        (await ListAsync(stationCodes, hours, cancellationToken)).Count;

    public async Task<IReadOnlyList<OpsJourneyBrief>> ListAffectedAsync(IReadOnlyCollection<string> stationCodes, int hours, int take, CancellationToken cancellationToken) =>
        (await ListAsync(stationCodes, hours, cancellationToken)).Take(take).ToList();

    private async Task<List<OpsJourneyBrief>> ListAsync(IReadOnlyCollection<string> stationCodes, int hours, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var date = timeZone.Today();
        var result = new List<OpsJourneyBrief>();

        foreach (var d in new[] { date, date.AddDays(1) })
        {
            var day = await reader.ListAsync(new OpsJourneyFilter(d, null, null, null), cancellationToken);
            result.AddRange(day
                .Where(j => PlanningRules.Active(j) && j.ScheduledArrivalUtc > now && j.ScheduledDepartureUtc <= now.AddHours(hours)
                    && j.Stops.Any(s => stationCodes.Contains(s.StationCode, StringComparer.Ordinal)))
                .Select(j => new OpsJourneyBrief(j.Id, j.TrainNumber, j.RouteCode, j.OriginStationCode, j.DestinationStationCode, j.ScheduledDepartureUtc, j.Lifecycle.ToString())));
        }

        return result.OrderBy(j => j.DepartsUtc).ToList();
    }
}

public sealed class JourneyControl(ISender sender) : IJourneyControl
{
    public async Task<JourneyControlResult> TerminateEmergencyAsync(Guid journeyId, string reason, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new TerminateEmergencyCommand(journeyId, reason), cancellationToken);
        return result.IsSuccess ? new JourneyControlResult(true, null, null) : new JourneyControlResult(false, result.Error.Code, result.Error.Description);
    }
}
