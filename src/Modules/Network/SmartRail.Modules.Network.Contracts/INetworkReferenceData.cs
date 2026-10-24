namespace SmartRail.Modules.Network.Contracts;

// The network's time zone (REQ-NFR-005). Times are stored in UTC and shown in this zone.
public interface INetworkTimeZone
{
    string TimeZoneId { get; }

    TimeZoneInfo Zone { get; }

    // Today's date in the network time zone.
    DateOnly Today();

    // The local date of an instant in the network time zone.
    DateOnly LocalDate(DateTimeOffset instant);

    // A service date plus minutes after its midnight (may exceed 1440), as a UTC instant.
    DateTimeOffset ToUtc(DateOnly serviceDate, int minutesAfterMidnight);
}

// Station names for other modules. Unknown or inactive codes are simply absent from the result.
public interface INetworkStations
{
    Task<IReadOnlyDictionary<string, string>> GetNamesAsync(IEnumerable<string> stationCodes, string? language, CancellationToken cancellationToken);

    // The platform codes of a station that can be used (available ones), in order. Null when the station is unknown.
    Task<IReadOnlyList<string>?> GetPlatformsAsync(string stationCode, CancellationToken cancellationToken);
}

// Route facts for other modules.
public interface INetworkRoutes
{
    // The station codes of an active route in forward order, passing points included. Null when the route is unknown.
    Task<IReadOnlyList<string>?> GetStationCodesAsync(string routeCode, CancellationToken cancellationToken);

    // The codes of every active route.
    Task<IReadOnlyList<string>> ListCodesAsync(CancellationToken cancellationToken);
}
