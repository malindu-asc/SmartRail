using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Network.Contracts;

namespace SmartRail.Modules.Network.Infrastructure.Persistence;

internal sealed class NetworkStations(NetworkDbContext context) : INetworkStations
{
    public async Task<IReadOnlyDictionary<string, string>> GetNamesAsync(
        IEnumerable<string> stationCodes, string? language, CancellationToken cancellationToken)
    {
        var codes = stationCodes.Distinct(StringComparer.Ordinal).ToList();
        if (codes.Count == 0)
            return new Dictionary<string, string>();

        var languages = await context.Languages.AsNoTracking().Select(l => new { l.Id, l.IsDefault }).ToListAsync(cancellationToken);
        var chosen = languages.FirstOrDefault(l => string.Equals(l.Id, language?.Trim(), StringComparison.OrdinalIgnoreCase));
        var languageCode = chosen is { IsDefault: false } ? chosen.Id : null;

        var stations = await context.Stations
            .AsNoTracking()
            .Include(s => s.Translations)
            .Where(s => s.IsActive && codes.Contains(s.Code))
            .ToListAsync(cancellationToken);

        return stations.ToDictionary(
            s => s.Code,
            s => languageCode is null
                ? s.Name
                : s.Translations.FirstOrDefault(t => string.Equals(t.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))?.Name ?? s.Name,
            StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<string>?> GetPlatformsAsync(string stationCode, CancellationToken cancellationToken)
    {
        var code = stationCode.Trim().ToUpperInvariant();
        var station = await context.Stations.AsNoTracking().Include(s => s.Platforms)
            .FirstOrDefaultAsync(s => s.IsActive && s.Code == code, cancellationToken);

        return station?.Platforms.Where(p => p.IsAvailable).Select(p => p.Code).OrderBy(c => c, StringComparer.Ordinal).ToList();
    }
}

internal sealed class NetworkRoutes(NetworkDbContext context) : INetworkRoutes
{
    public async Task<IReadOnlyList<string>?> GetStationCodesAsync(string routeCode, CancellationToken cancellationToken)
    {
        var code = routeCode.Trim().ToUpperInvariant();

        var route = await context.Routes
            .AsNoTracking()
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.IsActive && r.Code == code, cancellationToken);
        if (route is null)
            return null;

        var stationIds = route.Stops.Select(s => s.StationId).ToList();
        var codes = await context.Stations
            .AsNoTracking()
            .Where(s => stationIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Code, cancellationToken);

        return route.Stops.OrderBy(s => s.Sequence).Select(s => codes[s.StationId]).ToList();
    }

    public async Task<IReadOnlyList<string>> ListCodesAsync(CancellationToken cancellationToken) =>
        await context.Routes.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Code).Select(r => r.Code).ToListAsync(cancellationToken);
}
