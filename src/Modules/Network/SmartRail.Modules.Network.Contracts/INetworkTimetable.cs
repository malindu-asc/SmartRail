namespace SmartRail.Modules.Network.Contracts;

// What the Journeys module may ask the Network module about the timetable.
// This is the working view: passing points are included. Stations are referenced by code, never by internal id.
public interface INetworkTimetable
{
    // The services that run on a service date, taken from the timetable version in force on that date.
    // An empty list means no service runs (or no timetable covers the date).
    Task<IReadOnlyList<ScheduledService>> GetServicesRunningOnAsync(DateOnly serviceDate, CancellationToken cancellationToken);
}

public sealed record ScheduledService(
    Guid ServiceId,
    Guid TimetableVersionId,
    string TrainNumber,
    string RouteCode,
    IReadOnlyList<ScheduledStop> Stops);

// Times are minutes after midnight of the service date and may exceed 1440.
public sealed record ScheduledStop(
    int Sequence,
    string StationCode,
    int? ArrivalMinutes,
    int? DepartureMinutes,
    string? PlatformCode,
    string Kind);
