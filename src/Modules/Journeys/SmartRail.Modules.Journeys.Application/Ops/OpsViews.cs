using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Network.Contracts;

namespace SmartRail.Modules.Journeys.Application.Ops;

// ---- what the consoles read ----

// One line of the live board. Data says how fresh the live picture is: Live (feed within 60 s), Stale (60 s to 5 min),
// Unknown (nothing for 5 min), Scheduled (not started) or Confirmed (finished).
public sealed record OpsJourneyRow(
    Guid JourneyId,
    string TrainNumber,
    string RouteCode,
    string OriginCode,
    string OriginName,
    string DestinationCode,
    string DestinationName,
    DateTimeOffset ScheduledDepartureUtc,
    DateTimeOffset ExpectedDepartureUtc,
    string Lifecycle,
    string StatusCode,
    string StatusLabel,
    string StatusKind,
    string? NextStopCode,
    string? NextStopName,
    string? Platform,
    string? OriginalPlatform,
    string PlatformState,
    int? DelayMinutes,
    string? Punctuality,
    string Data,
    int? DataAgeSeconds,
    DateTimeOffset? LastUpdateUtc,
    string? DriverSubject,
    string? DriverName,
    Guid? UnitId,
    string? UnitCode,
    bool NeedsAttention,
    IReadOnlyList<string> Attention,
    int Version);

public sealed record OpsBoard(
    string TimeZone,
    DateTimeOffset AsOfUtc,
    DateOnly ServiceDate,
    int Total,
    int NeedAttention,
    IReadOnlyList<OpsJourneyRow> Items);

public sealed record OpsStopView(
    int Sequence,
    string StationCode,
    string StationName,
    string Kind,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ScheduledDepartureUtc,
    DateTimeOffset? ActualArrivalUtc,
    DateTimeOffset? ActualDepartureUtc,
    DateTimeOffset? ExpectedArrivalUtc,
    DateTimeOffset? ExpectedDepartureUtc,
    string? Platform,
    string? OriginalPlatform,
    string PlatformState,
    string State,
    string Source);

public sealed record OpsCrew(
    string? DriverSubject,
    string? DriverName,
    DateTimeOffset? DutyFromUtc,
    DateTimeOffset? DutyToUtc,
    Guid? UnitId,
    string? UnitCode,
    string? UnitName,
    string? LastDriverEvent);

// An action the screen may offer. Allowed says whether the journey's state permits it; Permission is what the user also needs.
public sealed record OpsAction(string Code, bool Allowed, string? WhyNot, string Permission);

public sealed record OpsJourneyDetail(
    OpsJourneyRow Journey,
    IReadOnlyList<OpsStopView> Stops,
    OpsCrew Crew,
    IReadOnlyList<OpsAction> Actions,
    string? CancelReason,
    string? EndReason,
    string? ControllerNote,
    DateTimeOffset? EndedAtUtc);

public sealed record OpsEventView(
    long Id,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ReceivedAtUtc,
    int StopSequence,
    string StationCode,
    string Text,
    string Source,
    string Disposition,
    string? Note);

public sealed record ReasonCode(string Code, string Label);

public sealed record ReasonCatalogView(
    IReadOnlyList<ReasonCode> Cancel,
    IReadOnlyList<ReasonCode> TerminateShort,
    IReadOnlyList<ReasonCode> Interrupt,
    IReadOnlyList<ReasonCode> Platform);

public static class ReasonCatalog
{
    public static readonly IReadOnlyList<ReasonCode> Cancel =
    [
        new("train_fault", "Train fault"), new("signal_fault", "Signal fault"), new("staff_unavailable", "Driver or crew unavailable"),
        new("unit_out_of_service", "Unit out of service"), new("infrastructure", "Track or infrastructure"),
        new("weather", "Weather"), new("safety", "Safety"), new("other", "Other")
    ];

    public static readonly IReadOnlyList<ReasonCode> TerminateShort =
    [
        new("train_fault", "Train fault"), new("signal_fault", "Signal fault"), new("line_blocked", "Line blocked"),
        new("staff_unavailable", "Driver or crew unavailable"), new("safety", "Safety"), new("other", "Other")
    ];

    public static readonly IReadOnlyList<ReasonCode> Interrupt =
    [
        new("train_fault", "Train fault"), new("signal_fault", "Signal fault"), new("line_blocked", "Line blocked"),
        new("safety", "Safety"), new("other", "Other")
    ];

    public static readonly IReadOnlyList<ReasonCode> Platform =
    [
        new("platform_blocked", "Platform blocked"), new("longer_train", "Longer train"), new("track_works", "Track works"),
        new("crowd_control", "Crowd control"), new("other", "Other")
    ];

    public static bool Valid(IReadOnlyList<ReasonCode> list, string code) =>
        list.Any(r => string.Equals(r.Code, code, StringComparison.Ordinal));
}

// ---- turning stored journeys into console rows ----

internal static class LocalTime
{
    // HH:mm in the railway's time zone (the server's own zone is never used for what people read).
    public static string Hm(this INetworkTimeZone zone, DateTimeOffset t) => TimeZoneInfo.ConvertTime(t, zone.Zone).ToString("HH:mm");
}

internal static class OpsMapper
{
    // How soon before departure a missing driver or unit starts to matter.
    public static readonly TimeSpan CrewHorizon = TimeSpan.FromHours(3);

    public static OpsJourneyRow ToRow(
        OpsJourneyData j,
        IReadOnlyDictionary<string, string> stationNames,
        IReadOnlyDictionary<string, string> driverNames,
        IReadOnlySet<Guid> unavailableUnits,
        DateTimeOffset now,
        LiveRules rules)
    {
        var live = LiveStatus.Evaluate(j.Lifecycle, j.Confidence, j.LastAcceptedUpdateAt, now, rules);
        var data = DataState(j, live);

        var next = j.Lifecycle == JourneyLifecycle.Running
            ? j.Stops.OrderBy(s => s.Sequence).FirstOrDefault(s => s.State == JourneyStopState.Pending && s.Kind != JourneyStopKind.Passing)
            : null;
        var origin = j.Stops.OrderBy(s => s.Sequence).First();

        string? platform = origin.PlatformCode;
        string? original = origin.PlatformState == PlatformState.Changed ? origin.PlatformOriginalCode : null;
        var platformState = origin.PlatformState;
        if (next is not null)
        {
            platform = next.PlatformCode;
            original = next.PlatformState == PlatformState.Changed ? next.PlatformOriginalCode : null;
            platformState = next.PlatformState;
        }

        var (code, label, kind) = Status(j, live, data, j.Stops.Any(s => s.PlatformState == PlatformState.Changed && s.State == JourneyStopState.Pending));

        var attention = new List<string>();
        if (j.Lifecycle is JourneyLifecycle.Running or JourneyLifecycle.Interrupted)
        {
            if (j.Punctuality == PunctualityBand.MajorDelay)
                attention.Add("MajorDelay");
            else if (j.DelayMinutes >= 5)
                attention.Add("Delayed");
            if (data == "Unknown")
                attention.Add("NoLiveData");
            else if (data == "Stale")
                attention.Add("StaleData");
            if (j.Lifecycle == JourneyLifecycle.Interrupted)
                attention.Add("Interrupted");
        }

        var upcoming = j.Lifecycle is JourneyLifecycle.Planned or JourneyLifecycle.Ready && j.ScheduledDepartureUtc - now <= CrewHorizon && j.ScheduledDepartureUtc > now - TimeSpan.FromMinutes(5);
        if (upcoming || j.Lifecycle is JourneyLifecycle.Running)
        {
            if (j.DutyId is null)
                attention.Add("NoDriver");
            if (j.VehicleId is null)
                attention.Add("NoUnit");
            else if (j.VehicleId is { } unit && unavailableUnits.Contains(unit))
                attention.Add("UnitOutOfService");
        }

        string Name(string c) => stationNames.GetValueOrDefault(c, c);
        var expected = ExpectedDeparture(j, live);

        return new OpsJourneyRow(
            j.Id, j.TrainNumber, j.RouteCode, j.OriginStationCode, Name(j.OriginStationCode), j.DestinationStationCode, Name(j.DestinationStationCode),
            j.ScheduledDepartureUtc, expected, j.Lifecycle.ToString(), code, label, kind,
            next?.StationCode, next is null ? null : Name(next.StationCode),
            platform, original, platformState.ToString(),
            live.ShowLiveTimes ? j.DelayMinutes : null, live.ShowLiveTimes ? j.Punctuality?.ToString() : null,
            data, live.AgeSeconds, j.LastAcceptedUpdateAt,
            j.DriverSubject, j.DriverSubject is null ? null : driverNames.GetValueOrDefault(j.DriverSubject), j.VehicleId, j.VehicleCode,
            attention.Count > 0, attention, j.Version);
    }

    private static DateTimeOffset ExpectedDeparture(OpsJourneyData j, LiveStatus live)
    {
        var origin = j.Stops.OrderBy(s => s.Sequence).First();
        if (origin.ActualDepartureUtc is { } actual)
            return actual;

        return live.ShowLiveTimes && origin.EstimatedDepartureUtc is { } estimate ? estimate : j.ScheduledDepartureUtc;
    }

    private static string DataState(OpsJourneyData j, LiveStatus live) => j.Lifecycle switch
    {
        JourneyLifecycle.Planned or JourneyLifecycle.Ready => "Scheduled",
        JourneyLifecycle.Completed or JourneyLifecycle.TerminatedShort => "Confirmed",
        JourneyLifecycle.Cancelled => "Scheduled",
        JourneyLifecycle.TerminatedEmergency => "Unknown",
        _ => live.Confidence == JourneyConfidence.Unknown ? "Unknown" : live.IsStale ? "Stale" : "Live"
    };

    // The words on the board and the colour family the chip uses (Good, Warning, Critical, Info, Stale, Neutral).
    private static (string Code, string Label, string Kind) Status(OpsJourneyData j, LiveStatus live, string data, bool platformChanged) => j.Lifecycle switch
    {
        JourneyLifecycle.Cancelled => ("Cancelled", "Cancelled", "Critical"),
        JourneyLifecycle.TerminatedShort => ("TerminatedShort", "Terminated short", "Critical"),
        JourneyLifecycle.TerminatedEmergency => ("TerminatedEmergency", "Emergency termination", "Critical"),
        JourneyLifecycle.Interrupted => ("Interrupted", "Interrupted", "Warning"),
        JourneyLifecycle.Completed => ("Completed", "Completed", "Neutral"),
        JourneyLifecycle.Running when data == "Unknown" => ("NoLiveData", "No live data", "Stale"),
        JourneyLifecycle.Running when data == "Stale" && j.DelayMinutes >= 2 => ("DelayedNoEstimate", "Delayed, no estimate", "Warning"),
        JourneyLifecycle.Running when j.DelayMinutes >= 2 => ("Delayed", "Delayed", "Warning"),
        JourneyLifecycle.Running => ("OnTime", "On time", "Good"),
        _ when platformChanged => ("PlatformChanged", "Platform changed", "Info"),
        _ => ("NotStarted", "Not started", "Neutral")
    };

    public static async Task<(IReadOnlyDictionary<string, string> Stations, IReadOnlyDictionary<string, string> Drivers)> NamesAsync(
        IEnumerable<OpsJourneyData> journeys, INetworkStations stations, IStaffNames staff, CancellationToken cancellationToken)
    {
        var list = journeys.ToList();
        var codes = list.SelectMany(j => j.Stops.Select(s => s.StationCode)).Distinct().ToList();
        var subjects = list.Select(j => j.DriverSubject).Where(s => s is not null).Select(s => s!).Distinct().ToList();
        return (await stations.GetNamesAsync(codes, null, cancellationToken), await staff.GetNamesAsync(subjects, cancellationToken));
    }
}
