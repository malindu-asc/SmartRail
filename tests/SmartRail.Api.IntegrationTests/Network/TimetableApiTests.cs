using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Stations;
using SmartRail.Modules.Network.Domain.Timetables;
using SmartRail.Modules.Network.Infrastructure.Extensions;
using SmartRail.Modules.Network.Infrastructure.Persistence;

namespace SmartRail.Api.IntegrationTests.Network;

internal static class TimetableJson
{
    internal static async Task<JsonElement> GetAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    internal static List<string> Numbers(JsonElement json) =>
        json.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("trainNumber").GetString()!).ToList();

    internal static async Task<string> ProblemCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("code").GetString()!;
    }
}

public sealed class TimetableApiTests(NetworkApiFactory factory) : IClassFixture<NetworkApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<JsonElement> DeparturesAsync(string station, string date) =>
        TimetableJson.GetAsync(_client, $"/api/network/stations/{station}/timetable?date={date}");

    private Task<JsonElement> ArrivalsAsync(string station, string date) =>
        TimetableJson.GetAsync(_client, $"/api/network/stations/{station}/timetable?date={date}&kind=arrivals");

    // AC-1
    [Fact]
    public async Task A_station_lists_departures_and_arrivals_sorted_by_time()
    {
        var departures = await DeparturesAsync("CEN", "2026-10-14");   // a Wednesday
        var items = departures.GetProperty("items").EnumerateArray().ToList();

        // CEN starts the weekday services of MAIN, HILL and COAST (4 each) plus the daily 20:00 MAIN and 23:50 HILL.
        Assert.Equal(14, items.Count);
        var minutes = items.Select(i => i.GetProperty("minutes").GetInt32()).ToList();
        Assert.Equal(minutes.Order().ToList(), minutes);
        Assert.All(items, i => Assert.False(string.IsNullOrEmpty(i.GetProperty("otherStationName").GetString())));
        Assert.Equal("Asia/Colombo", departures.GetProperty("timeZone").GetString());
        Assert.Equal("departures", departures.GetProperty("kind").GetString());

        // The reverse weekday services end at CEN, so they are arrivals only.
        var arrivals = await ArrivalsAsync("CEN", "2026-10-14");
        Assert.Equal(13, arrivals.GetProperty("items").GetArrayLength());
        var departureNumbers = TimetableJson.Numbers(departures);
        Assert.DoesNotContain(TimetableJson.Numbers(arrivals), departureNumbers.Contains);
    }

    // AC-2
    [Fact]
    public async Task Weekday_services_run_on_a_wednesday_and_not_on_a_saturday()
    {
        var wednesday = TimetableJson.Numbers(await DeparturesAsync("CEN", "2026-10-14"));
        var saturday = TimetableJson.Numbers(await DeparturesAsync("CEN", "2026-10-17"));

        Assert.Contains("1001", wednesday);
        Assert.DoesNotContain("1001", saturday);
        Assert.Equal(["D101", "D201", "W301"], saturday.Order().ToList());   // daily and weekend services only
    }

    // AC-3
    [Fact]
    public async Task A_holiday_removes_services_and_a_special_day_adds_them()
    {
        var holiday = TimetableJson.Numbers(await DeparturesAsync("CEN", "2026-10-23"));   // a Friday with a Removed exception
        Assert.Equal(["D101", "D201"], holiday.Order().ToList());

        var special = TimetableJson.Numbers(await DeparturesAsync("CEN", "2026-10-21"));   // a Wednesday with an Added exception
        Assert.Contains("W301", special);
        Assert.Contains("1001", special);
    }

    // AC-4
    [Fact]
    public async Task A_late_service_has_times_past_24_00_and_stays_on_its_service_date()
    {
        var departures = await DeparturesAsync("CEN", "2026-10-14");
        var late = departures.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("trainNumber").GetString() == "D201");
        Assert.Equal("23:50", late.GetProperty("scheduledTime").GetString());

        var serviceId = late.GetProperty("serviceId").GetGuid();
        var detail = await TimetableJson.GetAsync(_client, $"/api/network/services/{serviceId}?date=2026-10-14");
        var last = detail.GetProperty("stops").EnumerateArray().Last();
        Assert.Equal("24:24", last.GetProperty("scheduledArrival").GetString());
        Assert.Equal(1464, last.GetProperty("arrivalMinutes").GetInt32());
        Assert.Equal("2026-10-14", detail.GetProperty("date").GetString());
    }

    // AC-6
    [Fact]
    public async Task A_date_with_no_timetable_is_not_found()
    {
        using var response = await _client.GetAsync("/api/network/stations/CEN/timetable?date=2025-12-31");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Network.NoTimetableForDate", await TimetableJson.ProblemCodeAsync(response));
    }

    // AC-7
    [Fact]
    public async Task Service_detail_shows_public_stops_in_order_and_404_when_it_does_not_run()
    {
        var departures = await DeparturesAsync("CEN", "2026-10-14");
        var coast = departures.GetProperty("items").EnumerateArray().First(i => i.GetProperty("trainNumber").GetString() == "3001");
        var serviceId = coast.GetProperty("serviceId").GetGuid();

        var detail = await TimetableJson.GetAsync(_client, $"/api/network/services/{serviceId}?date=2026-10-14");
        var stops = detail.GetProperty("stops").EnumerateArray().ToList();
        Assert.Equal(["CEN", "LAK", "BAY"], stops.Select(s => s.GetProperty("stationCode").GetString()));   // Harbour is a passing point
        Assert.Equal([1, 2, 3], stops.Select(s => s.GetProperty("sequence").GetInt32()));
        Assert.True(stops[0].GetProperty("arrivalMinutes").ValueKind == JsonValueKind.Null);

        using var saturday = await _client.GetAsync($"/api/network/services/{serviceId}?date=2026-10-17");
        Assert.Equal(HttpStatusCode.NotFound, saturday.StatusCode);
        Assert.Equal("Network.ServiceNotRunning", await TimetableJson.ProblemCodeAsync(saturday));

        using var unknown = await _client.GetAsync($"/api/network/services/{Guid.NewGuid()}?date=2026-10-14");
        Assert.Equal("Network.ServiceNotFound", await TimetableJson.ProblemCodeAsync(unknown));
    }

    // AC-8 (the invalid part; the default-date part is in the fixed-clock class below)
    [Theory]
    [InlineData("/api/network/stations/CEN/timetable?date=14-10-2026")]
    [InlineData("/api/network/stations/CEN/timetable?date=2026-10-14&kind=both")]
    [InlineData("/api/network/services/00000000-0000-0000-0000-000000000001?date=yesterday")]
    public async Task An_invalid_date_or_kind_is_a_bad_request(string url)
    {
        using var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // AC-13
    [Fact]
    public async Task Journeys_can_read_the_working_timetable_through_the_contract()
    {
        using var warmUp = factory.CreateClient();   // starts the host

        await using var scope = factory.Services.CreateAsyncScope();
        var timetable = scope.ServiceProvider.GetRequiredService<INetworkTimetable>();

        var services = await timetable.GetServicesRunningOnAsync(new DateOnly(2026, 10, 14), default);

        Assert.Equal(35, services.Count);   // 32 weekday + 3 daily
        Assert.Single(services.Select(s => s.TimetableVersionId).Distinct());
        var coast = services.First(s => s.RouteCode == "COAST");
        Assert.Contains(coast.Stops, s => s.Kind == "Passing" && s.StationCode == "HAR");   // the working view keeps passing points
        Assert.All(services, s => Assert.All(s.Stops, st => Assert.Matches("^[A-Z]{3,5}$", st.StationCode)));

        Assert.Empty(await timetable.GetServicesRunningOnAsync(new DateOnly(2025, 12, 31), default));
    }

    // AC-14
    [Fact]
    public async Task The_new_endpoints_are_anonymous_and_cacheable()
    {
        using var response = await _client.GetAsync("/api/network/stations/CEN/timetable?date=2026-10-14");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("max-age=300", response.Headers.CacheControl!.ToString());
        var etag = response.Headers.ETag!.Tag;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/network/stations/CEN/timetable?date=2026-10-14");
        request.Headers.IfNoneMatch.ParseAdd(etag);
        using var second = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/identity/whoami")).StatusCode);
    }
}

// A fixed clock: 2026-10-14 03:00 UTC is 08:30 on Wednesday 14 October in Asia/Colombo.
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public sealed class FixedClockNetworkApiFactory : NetworkApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 10, 14, 3, 0, 0, TimeSpan.Zero)));
        });
    }
}

public sealed class TimetableDefaultDateTests(FixedClockNetworkApiFactory factory) : IClassFixture<FixedClockNetworkApiFactory>
{
    // AC-8
    [Fact]
    public async Task A_missing_date_means_today_in_the_network_time_zone()
    {
        using var client = factory.CreateClient();

        var json = await TimetableJson.GetAsync(client, "/api/network/stations/CEN/timetable");
        Assert.Equal("2026-10-14", json.GetProperty("date").GetString());
        Assert.Equal(14, json.GetProperty("items").GetArrayLength());
    }
}

public sealed class TimetableVersionResolutionTests(NetworkApiFactory factory) : IClassFixture<NetworkApiFactory>
{
    // AC-5
    [Fact]
    public async Task A_new_version_applies_only_from_its_effective_date()
    {
        using var client = factory.CreateClient();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NetworkDbContext>();
            var route = await db.Routes.Include(r => r.Stops).SingleAsync(r => r.Code == "MAIN");
            var calendar = await db.ServiceCalendars.SingleAsync(c => c.Code == "DAILY");
            var old = await db.TimetableVersions.SingleAsync();

            var winter = TimetableVersion.CreateDraft("Winter", "scheduler-a").Value;
            var stops = route.Stops.OrderBy(s => s.Sequence).Select((s, i) =>
                new ServiceStopDraft(i + 1, s.StationId,
                    i == 0 ? null : 480 + i * 10,
                    i == route.Stops.Count - 1 ? null : 480 + i * 10 + 1,
                    null, s.Kind)).ToList();
            winter.AddService(Service.Create(winter.Id, route, calendar.Id, "9999", ServiceDirection.Forward, ServiceCategory.Regional, stops).Value);
            winter.SubmitForReview();
            winter.Publish(new DateOnly(2026, 11, 1), "scheduler-b", DateTimeOffset.UtcNow);
            old.Supersede();

            db.TimetableVersions.Add(winter);
            await db.SaveChangesAsync();
        }

        // Before the effective date the old timetable still answers; from it, the new one.
        var before = TimetableJson.Numbers(await TimetableJson.GetAsync(client, "/api/network/stations/CEN/timetable?date=2026-10-31"));
        Assert.Equal(["D101", "D201", "W301"], before.Order().ToList());

        var onTheDay = TimetableJson.Numbers(await TimetableJson.GetAsync(client, "/api/network/stations/CEN/timetable?date=2026-11-01"));
        Assert.Equal(["9999"], onTheDay);

        // An earlier date, already past, is untouched by the new version.
        var wednesday = await TimetableJson.GetAsync(client, "/api/network/stations/CEN/timetable?date=2026-10-14");
        Assert.Equal(14, wednesday.GetProperty("items").GetArrayLength());
    }
}

public sealed class TimetableSeedTests(NetworkApiFactory factory) : IClassFixture<NetworkApiFactory>
{
    // AC-12
    [Fact]
    public async Task The_seed_is_complete_and_repeating_it_changes_nothing()
    {
        using var client = factory.CreateClient();

        async Task<(int versions, int services, int stops, int calendars, int exceptions)> CountAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NetworkDbContext>();
            return (
                await db.TimetableVersions.CountAsync(),
                await db.Services.CountAsync(),
                await db.Services.SelectMany(s => s.Stops).CountAsync(),
                await db.ServiceCalendars.CountAsync(),
                await db.ServiceCalendars.SelectMany(c => c.Exceptions).CountAsync());
        }

        var first = await CountAsync();
        Assert.Equal(1, first.versions);
        Assert.Equal(37, first.services);
        Assert.Equal(3, first.calendars);
        Assert.Equal(2, first.exceptions);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NetworkDbContext>();
            var version = await db.TimetableVersions.SingleAsync();
            Assert.Equal(TimetableVersionStatus.Published, version.Status);
            Assert.Equal(new DateOnly(2026, 1, 1), version.EffectiveFrom);
            Assert.NotEqual(version.Author, version.ApprovedBy);
        }

        await factory.Services.SeedNetworkReferenceDataAsync();
        Assert.Equal(first, await CountAsync());
    }
}

public sealed class TimetableSeedOffTests(UnseededNetworkApiFactory factory) : IClassFixture<UnseededNetworkApiFactory>
{
    // AC-12
    [Fact]
    public async Task Nothing_is_seeded_unless_the_flag_is_on()
    {
        using var client = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NetworkDbContext>();
        Assert.Equal(0, await db.Services.CountAsync());
        Assert.Equal(0, await db.ServiceCalendars.CountAsync());
        Assert.Equal(0, await db.TimetableVersions.CountAsync());
    }
}

public sealed class TimetableConstraintTests(NetworkApiFactory factory) : IClassFixture<NetworkApiFactory>
{
    // AC-11
    [Fact]
    public async Task Database_refuses_a_service_stop_with_a_platform_from_another_station()
    {
        using var client = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NetworkDbContext>();

        var a = Station.Create("TSA", "Test A", []).Value;
        var platformA = a.AddPlatform("1").Value;
        var b = Station.Create("TSB", "Test B", []).Value;
        b.AddPlatform("1");
        db.Stations.AddRange(a, b);

        var route = Route.Create("TESTRT", "Test route",
        [
            new RouteStopDraft(1, a.Id, null, 0, 1, StopKind.Stop),
            new RouteStopDraft(2, b.Id, null, 5, 1, StopKind.Stop)
        ]).Value;
        db.Routes.Add(route);

        var calendar = await db.ServiceCalendars.FirstAsync();
        var version = TimetableVersion.CreateDraft("Constraint test", "author").Value;
        var service = Service.Create(version.Id, route, calendar.Id, "T1", ServiceDirection.Forward, ServiceCategory.Regional,
        [
            new ServiceStopDraft(1, a.Id, null, 500, platformA.Id, StopKind.Stop),
            new ServiceStopDraft(2, b.Id, 505, null, platformA.Id, StopKind.Stop)   // platform A used at station B
        ]).Value;
        version.AddService(service);
        db.TimetableVersions.Add(version);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
