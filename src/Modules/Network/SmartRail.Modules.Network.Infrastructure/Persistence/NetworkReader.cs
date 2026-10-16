using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Domain.Stations;

namespace SmartRail.Modules.Network.Infrastructure.Persistence;

internal sealed class NetworkReader(NetworkDbContext context) : INetworkReader
{
    public async Task<IReadOnlyList<LanguageReadModel>> GetLanguagesAsync(CancellationToken cancellationToken) =>
        await context.Languages
            .AsNoTracking()
            .Select(l => new LanguageReadModel(l.Id, l.IsDefault))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StationReadModel>> GetActiveStationsAsync(CancellationToken cancellationToken)
    {
        var stations = await context.Stations
            .AsNoTracking()
            .Include(s => s.Platforms)
            .Include(s => s.Translations)
            .AsSplitQuery()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        return stations.Select(ToReadModel).ToList();
    }

    public async Task<StationReadModel?> GetActiveStationAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();

        var station = await context.Stations
            .AsNoTracking()
            .Include(s => s.Platforms)
            .Include(s => s.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.IsActive && s.Code == normalized, cancellationToken);

        return station is null ? null : ToReadModel(station);
    }

    public async Task<IReadOnlyList<RouteReadModel>> GetActiveRoutesAsync(CancellationToken cancellationToken)
    {
        var routes = await context.Routes
            .AsNoTracking()
            .Include(r => r.Stops)
            .AsSplitQuery()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        var lookup = await LoadStationLookupAsync(cancellationToken);
        return routes.Select(r => ToReadModel(r, lookup)).ToList();
    }

    public async Task<RouteReadModel?> GetActiveRouteAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();

        var route = await context.Routes
            .AsNoTracking()
            .Include(r => r.Stops)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.IsActive && r.Code == normalized, cancellationToken);

        if (route is null)
            return null;

        var lookup = await LoadStationLookupAsync(cancellationToken);
        return ToReadModel(route, lookup);
    }

    private async Task<StationLookup> LoadStationLookupAsync(CancellationToken cancellationToken)
    {
        // The network is small (tens of stations). Stations of inactive routes are not a concern for reads.
        var stations = await context.Stations
            .AsNoTracking()
            .Include(s => s.Platforms)
            .Include(s => s.Translations)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new StationLookup(stations.ToDictionary(s => s.Id));
    }

    private static StationReadModel ToReadModel(Station station) => new(
        station.Code,
        station.Name,
        station.Translations.ToDictionary(t => t.LanguageCode, t => t.Name, StringComparer.OrdinalIgnoreCase),
        station.Facilities,
        station.Platforms.OrderBy(p => p.Code, StringComparer.Ordinal)
            .Select(p => new PlatformReadModel(p.Code, p.IsAvailable))
            .ToList());

    private static RouteReadModel ToReadModel(Domain.Routes.Route route, StationLookup lookup) => new(
        route.Code,
        route.Name,
        route.Stops.OrderBy(s => s.Sequence).Select(s =>
        {
            var station = lookup.Stations[s.StationId];
            var platform = s.DefaultPlatformId is { } platformId
                ? station.Platforms.FirstOrDefault(p => p.Id == platformId)?.Code
                : null;

            return new RouteStopReadModel(
                s.Sequence,
                station.Code,
                station.Name,
                station.Translations.ToDictionary(t => t.LanguageCode, t => t.Name, StringComparer.OrdinalIgnoreCase),
                platform,
                s.MinutesFromPrevious,
                s.DwellMinutes,
                s.Kind.ToString());
        }).ToList());

    private sealed record StationLookup(IReadOnlyDictionary<Guid, Station> Stations);
}
