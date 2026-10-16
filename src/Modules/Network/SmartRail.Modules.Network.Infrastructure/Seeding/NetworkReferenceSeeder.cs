using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartRail.Modules.Network.Domain.Languages;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Stations;
using SmartRail.Modules.Network.Infrastructure.Persistence;

namespace SmartRail.Modules.Network.Infrastructure.Seeding;

// Insert-only: a row is added when its code is missing and is never changed afterwards,
// so anything edited later survives a restart. Ids are fixed so every environment matches.
public sealed class NetworkReferenceSeeder(NetworkDbContext context, ILogger<NetworkReferenceSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var added = new SeedCounts();

        await SeedLanguagesAsync(added, cancellationToken);
        await SeedStationsAsync(added, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await SeedRoutesAsync(added, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Network reference seed: added {Languages} languages, {Stations} stations, {Platforms} platforms, {Routes} routes; {Present} items were already present.",
            added.Languages, added.Stations, added.Platforms, added.Routes, added.Present);
    }

    private async Task SeedLanguagesAsync(SeedCounts counts, CancellationToken ct)
    {
        var existing = (await context.Languages.Select(l => l.Id).ToListAsync(ct)).ToHashSet();

        foreach (var (code, name, isDefault) in new[]
                 {
                     (NetworkSeedData.DefaultLanguage, "English", true),
                     (NetworkSeedData.SecondLanguage, "Sinhala", false)
                 })
        {
            if (existing.Contains(code)) { counts.Present++; continue; }
            context.Languages.Add(Language.Create(code, name, isDefault));
            counts.Languages++;
        }
    }

    private async Task SeedStationsAsync(SeedCounts counts, CancellationToken ct)
    {
        var existing = (await context.Stations.Select(s => s.Code).ToListAsync(ct)).ToHashSet();

        foreach (var seed in NetworkSeedData.Stations)
        {
            if (existing.Contains(seed.Code)) { counts.Present++; continue; }

            var station = Station.Create(seed.Code, seed.Name, seed.Facilities, SeedIds.For("station", seed.Code)).Value;
            foreach (var platform in seed.Platforms)
            {
                station.AddPlatform(platform, true, SeedIds.For("platform", seed.Code + "/" + platform));
                counts.Platforms++;
            }

            station.AddTranslation(NetworkSeedData.SecondLanguage, seed.Sinhala, NetworkSeedData.DefaultLanguage);
            context.Stations.Add(station);
            counts.Stations++;
        }
    }

    private async Task SeedRoutesAsync(SeedCounts counts, CancellationToken ct)
    {
        var existing = (await context.Routes.Select(r => r.Code).ToListAsync(ct)).ToHashSet();
        var stations = await context.Stations.Include(s => s.Platforms).ToDictionaryAsync(s => s.Code, ct);

        foreach (var seed in NetworkSeedData.Routes)
        {
            if (existing.Contains(seed.Code)) { counts.Present++; continue; }

            var drafts = seed.Stops.Select((stop, index) =>
            {
                var station = stations[stop.StationCode];
                Guid? platformId = stop.Platform is null
                    ? null
                    : station.Platforms.Single(p => p.Code == stop.Platform).Id;

                return new RouteStopDraft(index + 1, station.Id, platformId, stop.MinutesFromPrevious, stop.DwellMinutes, stop.Kind);
            }).ToList();

            var route = Route.Create(seed.Code, seed.Name, drafts, SeedIds.For("route", seed.Code));
            context.Routes.Add(route.Value);
            counts.Routes++;
        }
    }

    private sealed class SeedCounts
    {
        public int Languages, Stations, Platforms, Routes, Present;
    }

    private static class SeedIds
    {
        // Same text, same id, in every environment.
        internal static Guid For(string kind, string key) =>
            new(MD5.HashData(Encoding.UTF8.GetBytes($"smartrail:network:{kind}:{key}")));
    }
}
