using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Fleet;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Infrastructure.Persistence;
using SmartRail.Modules.Network.Contracts;

namespace SmartRail.Modules.Journeys.Infrastructure.Seeding;

// Demo data (flag Journeys:Seed:DemoFleet): 8 trainsets (one per route and direction), an early and a late shift per
// trainset and day, and an assignment for every journey that has none. Insert-only and idempotent.
public sealed class DemoFleetSeeder(
    JourneysDbContext context,
    INetworkRoutes routes,
    INetworkTimeZone timeZone,
    ILogger<DemoFleetSeeder> logger)
{
    // The demo driver user of the Keycloak realm. The other duties use placeholder subjects.
    private const string DemoDriverSubject = "a1000000-0000-4000-8000-000000000002";
    private const string Actor = "demo-seed";

    private static readonly string[] RouteCodes = ["MAIN", "HILL", "COAST", "NORTH"];

    private static readonly TimeSpan EarlyStart = new(5, 30, 0);
    private static readonly TimeSpan EarlyEnd = new(14, 0, 0);
    private static readonly TimeSpan LateEnd = new(24, 30, 0);   // 00:30 the next day

    public async Task SeedAsync(IReadOnlyList<DateOnly> dates, CancellationToken cancellationToken = default)
    {
        var vehicles = await EnsureVehiclesAsync(cancellationToken);
        var routeOrder = new Dictionary<string, string>();   // route code -> forward origin station code
        foreach (var code in RouteCodes)
        {
            var stations = await routes.GetStationCodesAsync(code, cancellationToken);
            if (stations is { Count: > 0 })
                routeOrder[code] = stations[0];
        }

        var assigned = 0;
        foreach (var date in dates)
        {
            var journeys = await context.Journeys
                .Include(j => j.VehicleAssignments)
                .Include(j => j.DriverAssignments)
                .Where(j => j.ServiceDate == date && j.Lifecycle == JourneyLifecycle.Planned)
                .ToListAsync(cancellationToken);

            var duties = await context.CrewDuties.Where(d => d.ServiceDate == date).ToListAsync(cancellationToken);

            foreach (var journey in journeys.Where(j => j.CreationOrigin == JourneyCreationOrigin.Generated))
            {
                if (!routeOrder.TryGetValue(journey.RouteCode, out var forwardOrigin))
                    continue;

                var forward = journey.OriginStationCode == forwardOrigin;
                var chain = $"{journey.RouteCode}-{(forward ? "F" : "R")}";

                if (!journey.HasVehicle)
                    journey.AssignVehicle(vehicles[chain].Id, journey.ScheduledDepartureUtc, journey.ScheduledArrivalUtc, Actor);

                if (!journey.HasDriver)
                {
                    var duty = GetOrAddDuty(duties, chain, date, journey);
                    journey.AssignDriver(duty.Id, journey.ScheduledDepartureUtc, journey.ScheduledArrivalUtc, Actor);
                }

                assigned++;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Demo fleet seed: {Journeys} journeys assigned", assigned);
    }

    private async Task<Dictionary<string, Vehicle>> EnsureVehiclesAsync(CancellationToken ct)
    {
        var existing = await context.Vehicles.ToDictionaryAsync(v => v.Code, ct);

        foreach (var route in RouteCodes)
        foreach (var (suffix, name) in new[] { ("F", "forward"), ("R", "reverse") })
        {
            var code = $"{route}-{suffix}";
            if (existing.ContainsKey(code))
                continue;

            var vehicle = Vehicle.Create(code, $"Trainset {route} {name}", VehicleKind.Trainset, SeedId("vehicle", code)).Value;
            context.Vehicles.Add(vehicle);
            existing[code] = vehicle;
        }

        await context.SaveChangesAsync(ct);
        return existing;
    }

    // Early shift 05:30 to 14:00 local, late shift 14:00 to 00:30 the next day. A journey belongs to the shift it ends in.
    private CrewDuty GetOrAddDuty(List<CrewDuty> duties, string chain, DateOnly date, Journey journey)
    {
        var earlyEnd = timeZone.ToUtc(date, (int)EarlyEnd.TotalMinutes);
        var early = journey.ScheduledArrivalUtc <= earlyEnd;

        var from = timeZone.ToUtc(date, (int)(early ? EarlyStart : EarlyEnd).TotalMinutes);
        var to = timeZone.ToUtc(date, (int)(early ? EarlyEnd : LateEnd).TotalMinutes);
        var subject = early && chain == "MAIN-F" ? DemoDriverSubject : $"seed-driver-{chain.ToLowerInvariant()}-{(early ? "early" : "late")}";

        var duty = duties.FirstOrDefault(d => d.DriverSubject == subject && d.RosteredFrom == from);
        if (duty is not null)
            return duty;

        duty = CrewDuty.Create(subject, date, from, to, SeedId("duty", $"{subject}:{date:yyyy-MM-dd}")).Value;
        context.CrewDuties.Add(duty);
        duties.Add(duty);
        return duty;
    }

    private static Guid SeedId(string kind, string key) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes($"smartrail:journeys:{kind}:{key}")));
}
