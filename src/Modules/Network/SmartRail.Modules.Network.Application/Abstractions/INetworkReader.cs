namespace SmartRail.Modules.Network.Application.Abstractions;

// Read side of the module. Queries use this instead of the unit of work; it returns plain read models.
public interface INetworkReader
{
    Task<IReadOnlyList<LanguageReadModel>> GetLanguagesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<StationReadModel>> GetActiveStationsAsync(CancellationToken cancellationToken);

    Task<StationReadModel?> GetActiveStationAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<RouteReadModel>> GetActiveRoutesAsync(CancellationToken cancellationToken);

    Task<RouteReadModel?> GetActiveRouteAsync(string code, CancellationToken cancellationToken);
}

public sealed record LanguageReadModel(string Code, bool IsDefault);

public sealed record PlatformReadModel(string Code, bool IsAvailable);

public sealed record StationReadModel(
    string Code,
    string Name,
    IReadOnlyDictionary<string, string> Translations,
    IReadOnlyList<string> Facilities,
    IReadOnlyList<PlatformReadModel> Platforms);

public sealed record RouteStopReadModel(
    int Sequence,
    string StationCode,
    string StationName,
    IReadOnlyDictionary<string, string> StationTranslations,
    string? DefaultPlatform,
    int MinutesFromPrevious,
    int DwellMinutes,
    string Kind);

public sealed record RouteReadModel(string Code, string Name, IReadOnlyList<RouteStopReadModel> Stops);
