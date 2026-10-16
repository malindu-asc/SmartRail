namespace SmartRail.Modules.Network.Application.Abstractions;

// Read side of the timetable. Returns plain read models for the version in force on a date.
public interface ITimetableReader
{
    // Null when no published timetable covers the date. Otherwise the services whose calendar runs that day.
    Task<TimetableDayReadModel?> GetDayAsync(DateOnly date, CancellationToken cancellationToken);

    Task<bool> ServiceExistsAsync(Guid serviceId, CancellationToken cancellationToken);
}

// The network's time zone and today's date in it (REQ-NFR-005).
public interface INetworkSettings
{
    string TimeZoneId { get; }

    DateOnly Today();
}

public sealed record TimetableDayReadModel(Guid VersionId, IReadOnlyList<ServiceReadModel> Services);

public sealed record ServiceReadModel(
    Guid ServiceId,
    string TrainNumber,
    string RouteCode,
    IReadOnlyList<ServiceStopReadModel> Stops);

public sealed record ServiceStopReadModel(
    int Sequence,
    string StationCode,
    string StationName,
    IReadOnlyDictionary<string, string> StationTranslations,
    int? ArrivalMinutes,
    int? DepartureMinutes,
    string? PlatformCode,
    string Kind);
