using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SmartRail.Api.IntegrationTests.Network;
using SmartRail.Modules.Journeys.Application.Generation;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Infrastructure.Generation;
using SmartRail.Modules.Journeys.Infrastructure.Persistence;
using SmartRail.Modules.Network.Contracts;

namespace SmartRail.Api.IntegrationTests.Journeys;

// 2026-10-14 03:00 UTC is 08:30 on Wednesday 14 October in Asia/Colombo.
public class JourneysApiFactory : NetworkApiFactory
{
    protected virtual DateTimeOffset Now => new(2026, 10, 14, 3, 0, 0, TimeSpan.Zero);

    protected virtual bool DemoFleet => false;

    // Drivers only see a day's duties once the scheduler approved the plan (production default); most tests switch this off.
    protected virtual bool ApprovalRequired => false;

    // A placeholder driver on every trainset, so every generated journey is Ready (the live feed only reaches Ready journeys).
    protected virtual bool AllChainsDriven => false;

    // The clock the API reads. Tests move it to age the live data (Stale at 60 s, Unknown at 300 s).
    public AdjustableTimeProvider Clock { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Journeys:Seed:DemoFleet"] = DemoFleet ? "true" : "false",
                ["Journeys:Seed:DemoFleetAllChains"] = AllChainsDriven ? "true" : "false",
                ["Journeys:Planning:ApprovalRequired"] = ApprovalRequired ? "true" : "false",

                // Two feed sources of different rank, for the precedence tests. Test keys only.
                ["Identity:FeedSources:Seed:0:Code"] = FeedKeys.Primary.Code,
                ["Identity:FeedSources:Seed:0:Name"] = "Primary test feed",
                ["Identity:FeedSources:Seed:0:Key"] = FeedKeys.Primary.Key,
                ["Identity:FeedSources:Seed:0:Rank"] = "1",
                ["Identity:FeedSources:Seed:1:Code"] = FeedKeys.Secondary.Code,
                ["Identity:FeedSources:Seed:1:Name"] = "Secondary test feed",
                ["Identity:FeedSources:Seed:1:Key"] = FeedKeys.Secondary.Key,
                ["Identity:FeedSources:Seed:1:Rank"] = "2"
            }));

        builder.ConfigureTestServices(services =>
        {
            Clock = new AdjustableTimeProvider(Now);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> _sequences = new();

    // Sequences are one counter per source and must never go backwards, so tests that share a database share the counter.
    internal long NextSequence(string source) => _sequences.AddOrUpdate(source, 1000, (_, last) => last + 1);

    internal async Task<GenerationResult> GenerateAsync()
    {
        using var warmUp = CreateClient();   // starts the host and applies the migrations
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<JourneyGenerationRunner>().RunAsync();
    }

    internal async Task<T> WithDbAsync<T>(Func<JourneysDbContext, Task<T>> action)
    {
        using var warmUp = CreateClient();
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<JourneysDbContext>());
    }
}

internal static class FeedKeys
{
    internal static readonly (string Code, string Key) Primary = ("SIM", "test-key-primary-0123456789");
    internal static readonly (string Code, string Key) Secondary = ("SIM-B", "test-key-secondary-0123456789");
}

public sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Set(DateTimeOffset value) => _now = value;

    public void Advance(TimeSpan by) => _now += by;
}

public sealed class DemoFleetApiFactory : JourneysApiFactory
{
    protected override bool DemoFleet => true;
}

// 19:30 on 14 October, network time: the 06:30, 08:00, 12:00 and 17:30 services are over, so tests can send their events
// (never in the future), and the 17:30 ones are still inside the board's 2 hour window for running trains.
public sealed class LiveIngestApiFactory : JourneysApiFactory
{
    protected override DateTimeOffset Now => new(2026, 10, 14, 14, 0, 0, TimeSpan.Zero);

    protected override bool DemoFleet => true;

    protected override bool AllChainsDriven => true;
}

// 00:05 on 14 October, network time: the 23:50 journeys of 13 October are still running.
public sealed class PostMidnightApiFactory : JourneysApiFactory
{
    protected override bool AllChainsDriven => true;

    protected override DateTimeOffset Now => new(2026, 10, 13, 18, 35, 0, TimeSpan.Zero);
}

// The tests in this class share one database, so only the first run in it creates anything: the idempotency test has its own class.
public sealed class JourneyIdempotencyTests(JourneysApiFactory factory) : IClassFixture<JourneysApiFactory>
{
    // Generation is idempotent: a second run creates nothing.
    [Fact]
    public async Task Generation_creates_the_horizon_once_and_a_second_run_changes_nothing()
    {
        var first = await factory.GenerateAsync();
        Assert.True(first.Created > 0);
        Assert.Equal(0, first.Failed);

        var count = await factory.WithDbAsync(db => db.Journeys.CountAsync());
        Assert.Equal(first.Created, count);

        var second = await factory.GenerateAsync();
        Assert.Equal(0, second.Created);
        Assert.Equal(first.Created, second.Skipped);
        Assert.Equal(count, await factory.WithDbAsync(db => db.Journeys.CountAsync()));
    }
}

public sealed class JourneyGenerationTests(JourneysApiFactory factory) : IClassFixture<JourneysApiFactory>
{
    [Fact]
    public async Task Generated_journeys_carry_the_scheduled_times_of_the_service_and_start_planned()
    {
        await factory.GenerateAsync();

        var journeys = await factory.WithDbAsync(db => db.Journeys.Include(j => j.Stops).Where(j => j.ServiceDate == new DateOnly(2026, 10, 14)).ToListAsync());

        Assert.Equal(35, journeys.Count);   // 32 weekday + 3 daily services, see the timetable tests
        Assert.All(journeys, j =>
        {
            Assert.Equal(JourneyLifecycle.Planned, j.Lifecycle);
            Assert.Equal(JourneyConfidence.Scheduled, j.Confidence);
            Assert.NotNull(j.ServiceId);
            Assert.True(j.ScheduledArrivalUtc > j.ScheduledDepartureUtc);
        });
    }

    // A new timetable version only applies to dates that have no journeys yet; existing journeys are left alone.
    [Fact]
    public async Task Existing_journeys_are_not_touched_by_a_later_run()
    {
        await factory.GenerateAsync();
        var before = await factory.WithDbAsync(db => db.Journeys.AsNoTracking().OrderBy(j => j.Id).Select(j => new { j.Id, j.Version, j.ScheduledDepartureUtc }).ToListAsync());

        await factory.GenerateAsync();
        var after = await factory.WithDbAsync(db => db.Journeys.AsNoTracking().OrderBy(j => j.Id).Select(j => new { j.Id, j.Version, j.ScheduledDepartureUtc }).ToListAsync());

        Assert.Equal(before, after);
    }
}

public sealed class JourneyEndpointTests(JourneysApiFactory factory) : IClassFixture<JourneysApiFactory>
{
    private async Task<(HttpClient Client, string Station)> ArrangeAsync()
    {
        await factory.GenerateAsync();
        return (factory.CreateClient(), "CEN");
    }

    [Fact]
    public async Task The_board_is_public_sorted_and_cacheable_for_five_seconds()
    {
        var (client, station) = await ArrangeAsync();

        using var response = await client.GetAsync($"/api/journeys/boards/{station}?count=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("max-age=5", response.Headers.CacheControl!.ToString());

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(5, items.Count);
        Assert.Equal("Asia/Colombo", json.GetProperty("timeZone").GetString());

        var times = items.Select(i => i.GetProperty("scheduledDepartureUtc").GetDateTimeOffset()).ToList();
        Assert.Equal(times.Order().ToList(), times);
        Assert.All(times, t => Assert.True(t >= new DateTimeOffset(2026, 10, 14, 3, 0, 0, TimeSpan.Zero).AddMinutes(-1)));
        Assert.All(items, i => Assert.Equal("Planned", i.GetProperty("status").GetString()));

        // Nothing has arrived yet, so no train is at the platform.
        Assert.All(items, i => Assert.False(i.GetProperty("atPlatform").GetBoolean()));

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/journeys/boards/{station}?count=5");
        request.Headers.IfNoneMatch.ParseAdd(response.Headers.ETag!.Tag);
        using var second = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }

    [Fact]
    public async Task The_board_rejects_an_unknown_station_and_a_bad_count()
    {
        var (client, _) = await ArrangeAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/journeys/boards/ZZZ")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/journeys/boards/CEN?count=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/journeys/boards/CEN?count=51")).StatusCode);
    }

    [Fact]
    public async Task The_arrivals_board_is_public_and_sorted_and_names_where_each_train_started()
    {
        var (client, _) = await ArrangeAsync();

        // Riverside is the end of the MAIN route, so trains arrive there and none of them is the first stop.
        var json = await client.GetFromJsonAsync<JsonElement>("/api/journeys/boards/RIV/arrivals?count=5");
        var items = json.GetProperty("items").EnumerateArray().ToList();

        Assert.NotEmpty(items);
        Assert.Equal("Riverside", json.GetProperty("station").GetProperty("name").GetString());

        var times = items.Select(i => i.GetProperty("scheduledArrivalUtc").GetDateTimeOffset()).ToList();
        Assert.Equal(times.Order().ToList(), times);
        Assert.All(items, i => Assert.False(string.IsNullOrEmpty(i.GetProperty("originName").GetString())));
        Assert.All(items, i => Assert.NotEqual("Riverside", i.GetProperty("originName").GetString()));
        Assert.All(items, i => Assert.Equal("Planned", i.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task The_arrivals_board_rejects_an_unknown_station_and_a_bad_count()
    {
        var (client, _) = await ArrangeAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/journeys/boards/ZZZ/arrivals")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/journeys/boards/RIV/arrivals?count=0")).StatusCode);
    }

    [Fact]
    public async Task A_journey_is_read_by_its_id_with_ordered_stops_and_names()
    {
        var (client, station) = await ArrangeAsync();
        var board = await client.GetFromJsonAsync<JsonElement>($"/api/journeys/boards/{station}?count=1");
        var id = board.GetProperty("items")[0].GetProperty("journeyId").GetGuid();

        var journey = await client.GetFromJsonAsync<JsonElement>($"/api/journeys/{id}");

        Assert.Equal(id, journey.GetProperty("journeyId").GetGuid());
        Assert.Equal("Planned", journey.GetProperty("status").GetString());
        Assert.Equal("Scheduled", journey.GetProperty("confidence").GetString());
        var stops = journey.GetProperty("stops").EnumerateArray().ToList();
        Assert.True(stops.Count >= 2);
        Assert.Equal(Enumerable.Range(1, stops.Count), stops.Select(s => s.GetProperty("sequence").GetInt32()));
        Assert.All(stops, s => Assert.False(string.IsNullOrEmpty(s.GetProperty("stationName").GetString())));

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/journeys/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task A_manual_journey_needs_the_scheduler_role_and_is_audited()
    {
        await factory.GenerateAsync();
        var routes = factory.Services.CreateScope().ServiceProvider.GetRequiredService<INetworkRoutes>();
        var stations = (await routes.GetStationCodesAsync("MAIN", default))!;

        var body = new
        {
            routeCode = "MAIN",
            trainNumber = "SP1",
            serviceDate = "2026-10-14",
            reason = "Festival special",
            stops = new object[]
            {
                new { stationCode = stations[0], departure = "22:00", platform = "1" },
                new { stationCode = stations[1], arrival = "22:20", departure = "22:22" },
                new { stationCode = stations[2], arrival = "22:45" }
            }
        };

        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/journeys/manual", body)).StatusCode);

        using var passenger = factory.CreateAuthenticatedClient(factory.Tokens.Issue(roles: ["passenger"]));
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.PostAsJsonAsync("/api/journeys/manual", body)).StatusCode);

        using var scheduler = factory.CreateAuthenticatedClient(factory.Tokens.Issue(roles: ["scheduler"]));
        using var created = await scheduler.PostAsJsonAsync("/api/journeys/manual", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("journeyId").GetGuid();

        var journey = await anonymous.GetFromJsonAsync<JsonElement>($"/api/journeys/{id}");
        Assert.Equal("SP1", journey.GetProperty("trainNumber").GetString());

        var audits = await factory.WithDbAsync(db => db.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM journeys.audit_entries WHERE target_id = {id.ToString()}")
            .SingleAsync());
        Assert.Equal(1, audits);
    }

    [Fact]
    public async Task A_manual_journey_must_follow_the_route_order_and_have_times_in_order()
    {
        await factory.GenerateAsync();
        var stations = (await factory.Services.CreateScope().ServiceProvider.GetRequiredService<INetworkRoutes>().GetStationCodesAsync("MAIN", default))!;
        using var scheduler = factory.CreateAuthenticatedClient(factory.Tokens.Issue(roles: ["scheduler"]));

        var outOfOrder = new
        {
            routeCode = "MAIN", trainNumber = "SP2", serviceDate = "2026-10-14", reason = "Test",
            stops = new object[]
            {
                new { stationCode = stations[0], departure = "22:00" },
                new { stationCode = stations[2], arrival = "22:20", departure = "22:22" },
                new { stationCode = stations[1], arrival = "22:45" }
            }
        };
        using var wrongOrder = await scheduler.PostAsJsonAsync("/api/journeys/manual", outOfOrder);
        Assert.Equal(HttpStatusCode.BadRequest, wrongOrder.StatusCode);

        var noReason = new { routeCode = "MAIN", trainNumber = "SP3", serviceDate = "2026-10-14", reason = "", stops = outOfOrder.stops };
        Assert.Equal(HttpStatusCode.BadRequest, (await scheduler.PostAsJsonAsync("/api/journeys/manual", noReason)).StatusCode);
    }
}

public sealed class DemoFleetTests(DemoFleetApiFactory factory) : IClassFixture<DemoFleetApiFactory>
{
    // With the demo fleet flag every generated journey has a trainset. The three demo drivers cover three trainsets, so those journeys
    // are Ready; the rest wait for the scheduler to assign a driver and stay Planned.
    [Fact]
    public async Task The_demo_fleet_gives_every_journey_a_trainset_and_three_drivers_cover_three_of_them()
    {
        await factory.GenerateAsync();

        var states = await factory.WithDbAsync(db => db.Journeys.GroupBy(j => j.Lifecycle).Select(g => new { g.Key, Count = g.Count() }).ToListAsync());
        Assert.Equal(2, states.Count);
        Assert.Contains(states, s => s.Key == JourneyLifecycle.Ready && s.Count > 0);
        Assert.Contains(states, s => s.Key == JourneyLifecycle.Planned && s.Count > 0);

        var drivers = await factory.WithDbAsync(db => db.CrewDuties.Select(d => d.DriverSubject).Distinct().CountAsync());
        Assert.Equal(3, drivers);
        Assert.Equal(0, await factory.WithDbAsync(db => db.Journeys.CountAsync(j => !j.VehicleAssignments.Any())));

        var vehicles = await factory.WithDbAsync(db => db.Vehicles.CountAsync());
        Assert.Equal(8, vehicles);
        var assignments = await factory.WithDbAsync(db => db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM journeys.vehicle_assignments").SingleAsync());

        await factory.GenerateAsync();
        Assert.Equal(8, await factory.WithDbAsync(db => db.Vehicles.CountAsync()));
        Assert.Equal(assignments, await factory.WithDbAsync(db => db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM journeys.vehicle_assignments").SingleAsync()));
    }

    // The database refuses double-booking, whatever the application does.
    [Fact]
    public async Task The_database_rejects_a_vehicle_on_two_journeys_at_once_and_overlapping_duties()
    {
        await factory.GenerateAsync();

        var overlapVehicle = await Assert.ThrowsAnyAsync<Exception>(() => factory.WithDbAsync(db => db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO journeys.vehicle_assignments (id, journey_id, vehicle_id, valid_from, valid_to, assigned_by)
            SELECT gen_random_uuid(), (SELECT j.id FROM journeys.journeys j WHERE j.id <> a.journey_id LIMIT 1), a.vehicle_id, a.valid_from, a.valid_to, 'test'
            FROM journeys.vehicle_assignments a LIMIT 1
            """)));
        Assert.Contains("ex_vehicle_assignments_vehicle", overlapVehicle.ToString());

        var overlapDuty = await Assert.ThrowsAnyAsync<DbUpdateException>(() => factory.WithDbAsync(async db =>
        {
            var existing = await db.CrewDuties.AsNoTracking().FirstAsync();
            db.CrewDuties.Add(CrewDuty.Create(existing.DriverSubject, existing.ServiceDate, existing.RosteredFrom, existing.RosteredTo).Value);
            return await db.SaveChangesAsync();
        }));
        Assert.Contains("ex_crew_duties_driver", overlapDuty.ToString());
    }

    [Fact]
    public async Task The_audit_log_cannot_be_changed_or_deleted()
    {
        await factory.GenerateAsync();
        await factory.WithDbAsync(db => db.Database.ExecuteSqlRawAsync(
            "INSERT INTO journeys.audit_entries (occurred_at, actor_subject, action, target_type) VALUES (now(), 'test', 'Test', 'Test')"));

        var update = await Assert.ThrowsAnyAsync<Exception>(() => factory.WithDbAsync(db => db.Database.ExecuteSqlRawAsync("UPDATE journeys.audit_entries SET action = 'X'")));
        Assert.Contains("append-only", update.ToString());
        var delete = await Assert.ThrowsAnyAsync<Exception>(() => factory.WithDbAsync(db => db.Database.ExecuteSqlRawAsync("DELETE FROM journeys.audit_entries")));
        Assert.Contains("append-only", delete.ToString());
    }
}

public sealed class PostMidnightBoardTests(PostMidnightApiFactory factory) : IClassFixture<PostMidnightApiFactory>
{
    // At 00:05 the trains that left on the previous service date are still on the board.
    [Fact]
    public async Task A_journey_of_the_previous_service_date_still_shows_after_midnight()
    {
        using var warmUp = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new GenerateJourneysCommand(new DateOnly(2026, 10, 13), 1));

        var now = new DateTimeOffset(2026, 10, 13, 18, 35, 0, TimeSpan.Zero);
        var late = await factory.WithDbAsync(db => db.Journeys
            .Where(j => j.ServiceDate == new DateOnly(2026, 10, 13))
            .SelectMany(j => j.Stops, (j, s) => new { j.Id, s.StationCode, s.ScheduledDepartureUtc, s.Kind })
            .Where(x => x.ScheduledDepartureUtc > now && x.Kind == JourneyStopKind.Stop)
            .FirstOrDefaultAsync());
        Assert.NotNull(late);

        var board = await warmUp.GetFromJsonAsync<JsonElement>($"/api/journeys/boards/{late.StationCode}?count=50");
        var ids = board.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("journeyId").GetGuid()).ToList();

        Assert.Contains(late.Id, ids);
    }
}
