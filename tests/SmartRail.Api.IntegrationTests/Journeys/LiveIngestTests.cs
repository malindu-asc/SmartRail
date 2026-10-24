using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Modules.Journeys.Domain.Fleet;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;

namespace SmartRail.Api.IntegrationTests.Journeys;

internal sealed record StopInfo(int Sequence, string Code, DateTimeOffset? Arrival, DateTimeOffset? Departure);

internal sealed record JourneyInfo(Guid Id, IReadOnlyList<StopInfo> Stops)
{
    internal DateTimeOffset Departure => Stops[0].Departure!.Value;
}

// SPEC-005: the train detection feed through the real API, on a real Postgres. Every journey is used by one test only.
public sealed class LiveIngestTests(LiveIngestApiFactory factory) : IClassFixture<LiveIngestApiFactory>
{
    private static readonly SemaphoreSlim PoolLock = new(1, 1);
    private static List<JourneyInfo>? _pool;

    private HttpClient Feed((string Code, string Key) source)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Source-Code", source.Code);
        client.DefaultRequestHeaders.Add("X-Source-Key", source.Key);
        return client;
    }

    private long Seq((string Code, string Key) source) => factory.NextSequence(source.Code);

    private static object Event(long sequence, Guid journey, int stop, string type, DateTimeOffset at) =>
        new { sequence, journeyId = journey, stopSequence = stop, type, occurredAt = at };

    private static async Task<JsonElement> PostAsync(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<JsonElement> SendEventsAsync(HttpClient client, params object[] events) =>
        PostAsync(client, "/api/ingest/stop-events", new { events });

    private static string Disposition(JsonElement response, int index = 0) =>
        response.GetProperty("results")[index].GetProperty("disposition").GetString()!;

    private static string? Reason(JsonElement response, int index = 0) =>
        response.GetProperty("results")[index].GetProperty("reason").GetString();

    // Ready journeys that are over by the fixed clock, so any event inside them is in the past.
    private Task<JourneyInfo> TakeJourneyAsync() => TakeAsync(recent: false);

    // The latest finished journey that is still inside the board's 2 hour window for running trains.
    private Task<JourneyInfo> TakeRecentJourneyAsync() => TakeAsync(recent: true);

    private async Task<JourneyInfo> TakeAsync(bool recent)
    {
        await PoolLock.WaitAsync();
        try
        {
            if (_pool is null)
            {
                await factory.GenerateAsync();
                var limit = factory.Clock.GetUtcNow().AddMinutes(-40);
                var journeys = await factory.WithDbAsync(db => db.Journeys
                    .AsNoTracking().Include(j => j.Stops)
                    .Where(j => j.Lifecycle == JourneyLifecycle.Ready && j.ScheduledArrivalUtc <= limit)
                    .OrderBy(j => j.ScheduledDepartureUtc).ThenBy(j => j.TrainNumber)
                    .ToListAsync());

                _pool = new List<JourneyInfo>(journeys
                    .Where(j => j.Stops.Count >= 4)   // the position test needs a section two stops further on
                    .Select(j => new JourneyInfo(j.Id, j.Stops.OrderBy(s => s.Sequence)
                        .Select(s => new StopInfo(s.Sequence, s.StationCode, s.ScheduledArrivalUtc, s.ScheduledDepartureUtc)).ToList())));
            }

            Assert.True(_pool.Count > 0, "The test journey pool is empty; the fixed clock needs more finished journeys.");

            var window = factory.Clock.GetUtcNow().AddHours(-2);
            var index = recent ? _pool.FindLastIndex(j => j.Stops[1].Departure >= window) : 0;
            Assert.True(index >= 0, "No finished journey is recent enough for the board window.");

            var taken = _pool[index];
            _pool.RemoveAt(index);
            return taken;
        }
        finally
        {
            PoolLock.Release();
        }
    }

    private async Task<JsonElement> GetJourneyAsync(Guid id)
    {
        using var client = factory.CreateClient();
        return await client.GetFromJsonAsync<JsonElement>($"/api/journeys/{id}");
    }

    private static JsonElement PublicStop(JsonElement journey, int index) => journey.GetProperty("stops")[index];

    // ---- AC-1 ----

    [Fact]
    public async Task Only_a_known_active_source_with_the_right_key_is_let_in()
    {
        var journey = await TakeJourneyAsync();
        var body = new { events = new[] { Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure) } };

        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/ingest/stop-events", body)).StatusCode);

        using var wrongKey = Feed((FeedKeys.Primary.Code, "not-the-key"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await wrongKey.PostAsJsonAsync("/api/ingest/stop-events", body)).StatusCode);

        using var unknownCode = Feed(("NOPE", FeedKeys.Primary.Key));
        Assert.Equal(HttpStatusCode.Unauthorized, (await unknownCode.PostAsJsonAsync("/api/ingest/stop-events", body)).StatusCode);

        // A signed-in user is not a feed source.
        using var user = factory.CreateAuthenticatedClient(factory.Tokens.Issue(roles: ["controller", "admin"]));
        Assert.Equal(HttpStatusCode.Unauthorized, (await user.PostAsJsonAsync("/api/ingest/stop-events", body)).StatusCode);

        Assert.Equal(0, await factory.WithDbAsync(db => db.JourneyEvents.CountAsync(e => e.JourneyId == journey.Id)));

        using var good = Feed(FeedKeys.Primary);
        var response = await SendEventsAsync(good, Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure));
        Assert.Equal("Applied", Disposition(response));

        var stored = await factory.WithDbAsync(db => db.JourneyEvents.AsNoTracking().SingleAsync(e => e.JourneyId == journey.Id));
        Assert.Equal(EventSourceKind.Feed, stored.SourceKind);
        Assert.NotEqual(Guid.Empty, stored.SourceId);
    }

    // ---- AC-2, AC-3, AC-17, AC-22 ----

    [Fact]
    public async Task A_late_departure_makes_the_journey_running_with_a_delay_estimates_and_sources_and_the_schedule_is_untouched()
    {
        var journey = await TakeJourneyAsync();
        var before = await GetJourneyAsync(journey.Id);
        Assert.Equal("Scheduled", before.GetProperty("confidence").GetString());

        using var feed = Feed(FeedKeys.Primary);
        var response = await SendEventsAsync(feed, Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure.AddMinutes(8)));
        Assert.Equal("Applied", Disposition(response));

        var after = await GetJourneyAsync(journey.Id);
        Assert.Equal("Running", after.GetProperty("status").GetString());
        Assert.Equal(8, after.GetProperty("delayMinutes").GetInt32());
        Assert.Equal("MinorDelay", after.GetProperty("punctuality").GetString());
        Assert.Equal("Live", after.GetProperty("confidence").GetString());
        Assert.False(after.GetProperty("isStale").GetBoolean());

        var first = PublicStop(after, 0);
        Assert.Equal("Departed", first.GetProperty("state").GetString());
        Assert.Equal("Confirmed", first.GetProperty("estimateSource").GetString());

        var next = PublicStop(after, 1);
        Assert.Equal("Live", next.GetProperty("estimateSource").GetString());
        Assert.Equal(journey.Stops[1].Arrival!.Value.AddMinutes(8), next.GetProperty("estimatedArrivalUtc").GetDateTimeOffset());

        // The scheduled times never change, and no estimate is earlier than its schedule for departures (REQ-LIVE-012).
        for (var i = 0; i < before.GetProperty("stops").GetArrayLength(); i++)
        {
            var stopBefore = PublicStop(before, i);
            var stopAfter = PublicStop(after, i);
            Assert.Equal(stopBefore.GetProperty("scheduledDepartureUtc").ToString(), stopAfter.GetProperty("scheduledDepartureUtc").ToString());
            Assert.Equal(stopBefore.GetProperty("scheduledArrivalUtc").ToString(), stopAfter.GetProperty("scheduledArrivalUtc").ToString());

            Assert.False(string.IsNullOrEmpty(stopAfter.GetProperty("estimateSource").GetString()));
            if (stopAfter.GetProperty("estimatedDepartureUtc").ValueKind == JsonValueKind.String)
                Assert.True(stopAfter.GetProperty("estimatedDepartureUtc").GetDateTimeOffset() >= stopAfter.GetProperty("scheduledDepartureUtc").GetDateTimeOffset());
        }
    }

    // ---- AC-5, AC-6, AC-7, AC-21 ----

    [Fact]
    public async Task A_duplicate_changes_nothing_and_an_old_sequence_is_rejected_as_out_of_order()
    {
        var journey = await TakeJourneyAsync();
        using var feed = Feed(FeedKeys.Primary);

        var oldSequence = Seq(FeedKeys.Primary);
        var sequence = Seq(FeedKeys.Primary);
        var first = await SendEventsAsync(feed, Event(sequence, journey.Id, 1, "departed", journey.Departure.AddMinutes(3)));
        Assert.Equal("Applied", Disposition(first));
        var rows = await factory.WithDbAsync(db => db.JourneyEvents.CountAsync(e => e.JourneyId == journey.Id));

        var again = await SendEventsAsync(feed, Event(sequence, journey.Id, 1, "departed", journey.Departure.AddMinutes(30)));
        Assert.Equal("Duplicate", Disposition(again));
        Assert.Equal(rows, await factory.WithDbAsync(db => db.JourneyEvents.CountAsync(e => e.JourneyId == journey.Id)));
        Assert.Equal(3, (await GetJourneyAsync(journey.Id)).GetProperty("delayMinutes").GetInt32());   // the replay did not move it

        var late = await SendEventsAsync(feed, Event(oldSequence, journey.Id, 2, "arrived", journey.Stops[1].Arrival!.Value));
        Assert.Equal("Rejected", Disposition(late));
        Assert.Equal("out_of_order", Reason(late));

        var stored = await factory.WithDbAsync(db => db.JourneyEvents.AsNoTracking().SingleAsync(e => e.DeviceSequence == oldSequence));
        Assert.Equal(EventDisposition.Rejected, stored.Disposition);
        Assert.Equal("out_of_order", stored.DispositionReason);
    }

    [Fact]
    public async Task An_event_from_the_future_and_a_batch_with_one_bad_item_are_handled_item_by_item()
    {
        var journey = await TakeJourneyAsync();
        using var feed = Feed(FeedKeys.Primary);
        var now = factory.Clock.GetUtcNow();

        var response = await SendEventsAsync(feed,
            Event(Seq(FeedKeys.Primary), Guid.NewGuid(), 1, "departed", journey.Departure),               // no such journey
            Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", now.AddMinutes(10)),                    // from the future
            Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure));                    // fine

        Assert.Equal("Rejected", Disposition(response, 0));
        Assert.Equal("unknown_journey", Reason(response, 0));
        Assert.Equal("Rejected", Disposition(response, 1));
        Assert.Equal("implausible", Reason(response, 1));
        Assert.Equal("Applied", Disposition(response, 2));
    }

    [Fact]
    public async Task A_batch_of_more_than_100_is_refused()
    {
        var journey = await TakeJourneyAsync();
        using var feed = Feed(FeedKeys.Primary);

        var events = Enumerable.Range(0, 101).Select(i => Event(i + 1, journey.Id, 1, "departed", journey.Departure)).ToArray();
        using var response = await feed.PostAsJsonAsync("/api/ingest/stop-events", new { events });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- AC-8 ----

    [Fact]
    public async Task The_better_ranked_source_wins_and_the_loser_is_kept()
    {
        var journey = await TakeJourneyAsync();
        using var primary = Feed(FeedKeys.Primary);
        using var secondary = Feed(FeedKeys.Secondary);
        await SendEventsAsync(primary, Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure));
        var arrival = journey.Stops[1].Arrival!.Value;

        // The worse-ranked source speaks first with 08:07:00-style time; the better one then corrects it with 08:07:30-style time.
        var worse = await SendEventsAsync(secondary, Event(Seq(FeedKeys.Secondary), journey.Id, 2, "arrived", arrival.AddMinutes(5)));
        Assert.Equal("Applied", Disposition(worse));
        Assert.Equal(5, (await GetJourneyAsync(journey.Id)).GetProperty("delayMinutes").GetInt32());

        var better = await SendEventsAsync(primary, Event(Seq(FeedKeys.Primary), journey.Id, 2, "arrived", arrival.AddMinutes(2)));
        Assert.Equal("Applied", Disposition(better));
        Assert.Equal(2, (await GetJourneyAsync(journey.Id)).GetProperty("delayMinutes").GetInt32());

        var events = await factory.WithDbAsync(db => db.JourneyEvents.AsNoTracking()
            .Where(e => e.JourneyId == journey.Id && e.StopSequence == 2 && e.EventType == StopEventType.Arrived).OrderBy(e => e.Id).ToListAsync());
        Assert.Equal(2, events.Count);
        Assert.Equal(EventDisposition.Superseded, events[0].Disposition);          // the loser is kept
        Assert.Equal(events[1].Id, events[0].SupersededByEventId);
        Assert.Equal(EventDisposition.Applied, events[1].Disposition);

        // And the other way round: a worse-ranked message after a better one changes nothing.
        var later = await SendEventsAsync(secondary, Event(Seq(FeedKeys.Secondary), journey.Id, 2, "arrived", arrival.AddMinutes(9)));
        Assert.Equal("Superseded", Disposition(later));
        Assert.Equal(2, (await GetJourneyAsync(journey.Id)).GetProperty("delayMinutes").GetInt32());

        // The same rank cannot rewrite a recorded fact.
        var same = await SendEventsAsync(primary, Event(Seq(FeedKeys.Primary), journey.Id, 2, "arrived", arrival.AddMinutes(1)));
        Assert.Equal("Rejected", Disposition(same));
        Assert.Equal("already_recorded", Reason(same));
    }

    // ---- AC-9, AC-10, AC-18 (board) ----

    [Fact]
    public async Task A_journey_that_reaches_its_last_stop_completes_and_leaves_the_board()
    {
        var journey = await TakeJourneyAsync();
        using var feed = Feed(FeedKeys.Primary);
        var last = journey.Stops[^1];

        // Stops in between are never reported: the final arrival resolves them (AC-10).
        var response = await SendEventsAsync(feed,
            Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure),
            Event(Seq(FeedKeys.Primary), journey.Id, last.Sequence, "arrived", last.Arrival!.Value.AddMinutes(3)));

        Assert.Equal("Applied", Disposition(response, 1));

        var done = await GetJourneyAsync(journey.Id);
        Assert.Equal("Confirmed", done.GetProperty("confidence").GetString());
        Assert.Equal(3, done.GetProperty("delayMinutes").GetInt32());
        Assert.All(done.GetProperty("stops").EnumerateArray(), s =>
        {
            Assert.Contains(s.GetProperty("state").GetString(), new[] { "Departed", "Arrived" });
            Assert.Equal("Confirmed", s.GetProperty("estimateSource").GetString());
        });

        var middle = PublicStop(done, 1);
        Assert.Equal(JsonValueKind.Null, middle.GetProperty("actualArrivalUtc").ValueKind);   // inferred, not invented

        var inferred = await factory.WithDbAsync(db => db.JourneyEvents.AsNoTracking().SingleAsync(e => e.JourneyId == journey.Id && e.StopSequence == last.Sequence));
        Assert.Contains("inferred", inferred.Payload);

        using var client = factory.CreateClient();
        var board = await client.GetFromJsonAsync<JsonElement>($"/api/journeys/boards/{journey.Stops[0].Code}?count=50");
        Assert.DoesNotContain(board.GetProperty("items").EnumerateArray(), i => i.GetProperty("journeyId").GetGuid() == journey.Id);
    }

    // ---- AC-11, AC-12 ----

    [Fact]
    public async Task A_journey_without_vehicle_and_driver_holds_the_event_and_a_cancelled_one_refuses_it()
    {
        // A manual journey is Planned: nobody is assigned to it.
        var routes = factory.Services.CreateScope().ServiceProvider.GetRequiredService<SmartRail.Modules.Network.Contracts.INetworkRoutes>();
        var stations = (await routes.GetStationCodesAsync("MAIN", default))!;
        using var scheduler = factory.CreateAuthenticatedClient(factory.Tokens.Issue(roles: ["scheduler"]));
        using var created = await scheduler.PostAsJsonAsync("/api/journeys/manual", new
        {
            routeCode = "MAIN", trainNumber = "HOLD1", serviceDate = "2026-10-14", reason = "Held event test",
            stops = new object[]
            {
                new { stationCode = stations[0], departure = "04:00" },
                new { stationCode = stations[1], arrival = "04:20", departure = "04:22" },
                new { stationCode = stations[2], arrival = "04:45" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var planned = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("journeyId").GetGuid();

        using var feed = Feed(FeedKeys.Primary);
        var held = await SendEventsAsync(feed, Event(Seq(FeedKeys.Primary), planned, 1, "departed", new DateTimeOffset(2026, 10, 13, 22, 31, 0, TimeSpan.Zero)));
        Assert.Equal("HeldForCorrection", Disposition(held));
        Assert.Equal("Planned", (await GetJourneyAsync(planned)).GetProperty("status").GetString());

        var journey = await TakeJourneyAsync();
        await factory.WithDbAsync(async db =>
        {
            var entity = await db.Journeys.Include(j => j.Stops).SingleAsync(j => j.Id == journey.Id);
            entity.Cancel("STAFF");
            return await db.SaveChangesAsync();
        });

        var refused = await SendEventsAsync(feed, Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure));
        Assert.Equal("Rejected", Disposition(refused));
        Assert.Equal("journey_ended", Reason(refused));
        Assert.Equal(1, await factory.WithDbAsync(db => db.JourneyEvents.CountAsync(e => e.JourneyId == journey.Id)));   // kept in history
    }

    // ---- AC-13, AC-14 ----

    [Fact]
    public async Task Live_data_goes_stale_at_60_seconds_unknown_at_300_and_recovers_with_a_new_update()
    {
        var journey = await TakeRecentJourneyAsync();
        using var feed = Feed(FeedKeys.Primary);
        var now = factory.Clock.GetUtcNow();

        await SendEventsAsync(feed, Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure.AddMinutes(8)));

        var fresh = await GetJourneyAsync(journey.Id);
        Assert.False(fresh.GetProperty("isStale").GetBoolean());
        Assert.Equal(8, fresh.GetProperty("delayMinutes").GetInt32());

        factory.Clock.Set(now.AddSeconds(61));
        var stale = await GetJourneyAsync(journey.Id);
        Assert.True(stale.GetProperty("isStale").GetBoolean());
        Assert.Equal(61, stale.GetProperty("ageSeconds").GetInt32());
        Assert.Equal("Live", stale.GetProperty("confidence").GetString());
        Assert.Equal(8, stale.GetProperty("delayMinutes").GetInt32());

        factory.Clock.Set(now.AddSeconds(301));
        var unknown = await GetJourneyAsync(journey.Id);
        Assert.Equal("Unknown", unknown.GetProperty("confidence").GetString());
        Assert.Equal(JsonValueKind.Null, unknown.GetProperty("delayMinutes").ValueKind);
        var next = PublicStop(unknown, 1);
        Assert.Equal("Scheduled", next.GetProperty("estimateSource").GetString());
        Assert.Equal(JsonValueKind.Null, next.GetProperty("estimatedArrivalUtc").ValueKind);

        // The board follows: the row is back on its scheduled time and says so.
        using var client = factory.CreateClient();
        var board = await client.GetFromJsonAsync<JsonElement>($"/api/journeys/boards/{journey.Stops[1].Code}?count=50");
        var row = board.GetProperty("items").EnumerateArray().FirstOrDefault(i => i.GetProperty("journeyId").GetGuid() == journey.Id);
        Assert.Equal(JsonValueKind.Object, row.ValueKind);   // a running train that has not reached the stop yet stays on the board
        Assert.Equal("Unknown", row.GetProperty("confidence").GetString());
        Assert.Equal("Scheduled", row.GetProperty("estimateSource").GetString());
        Assert.Equal(row.GetProperty("scheduledDepartureUtc").GetDateTimeOffset(), row.GetProperty("expectedDepartureUtc").GetDateTimeOffset());

        // A new accepted update brings it back to Live.
        factory.Clock.Set(now.AddSeconds(310));
        var arrival = journey.Stops[1].Arrival!.Value.AddMinutes(8);
        await SendEventsAsync(feed, Event(Seq(FeedKeys.Primary), journey.Id, 2, "arrived", arrival));
        var back = await GetJourneyAsync(journey.Id);
        Assert.Equal("Live", back.GetProperty("confidence").GetString());
        Assert.False(back.GetProperty("isStale").GetBoolean());
        Assert.Equal(8, back.GetProperty("delayMinutes").GetInt32());

        factory.Clock.Set(now);
    }

    // ---- AC-15, AC-16 ----

    [Fact]
    public async Task Positions_reach_the_journey_through_the_vehicle_assignment_and_survive_a_substitution()
    {
        var journey = await TakeJourneyAsync();
        using var feed = Feed(FeedKeys.Primary);
        await SendEventsAsync(feed, Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure));

        var vehicle = await factory.WithDbAsync(db => db.Database
            .SqlQuery<string>($"SELECT v.code AS \"Value\" FROM journeys.vehicle_assignments a JOIN journeys.vehicles v ON v.id = a.vehicle_id WHERE a.journey_id = {journey.Id}")
            .FirstAsync());

        Task<JsonElement> Position(string code, DateTimeOffset at, int section, decimal progress, decimal speed = 70m) =>
            PostAsync(feed, "/api/ingest/positions", new
            {
                readings = new[]
                {
                    new
                    {
                        sequence = Seq(FeedKeys.Primary), vehicleCode = code, recordedAt = at,
                        fromStation = journey.Stops[section].Code, toStation = journey.Stops[section + 1].Code, progress, speedKmh = speed
                    }
                }
            });

        var at = journey.Departure.AddMinutes(4);
        var first = await Position(vehicle, at, 0, 0.30m);
        Assert.Equal("Applied", Disposition(first));
        Assert.Equal(journey.Id, first.GetProperty("results")[0].GetProperty("journeyId").GetGuid());

        // Going back within the section, jumping over a section, or flying: all implausible.
        var back = await Position(vehicle, at.AddSeconds(30), 0, 0.10m);
        Assert.Equal("implausible", Reason(back));

        var jump = await Position(vehicle, at.AddSeconds(40), 2, 0.10m);
        Assert.Equal("implausible", Reason(jump));

        var fast = await Position(vehicle, at.AddSeconds(50), 0, 0.50m, speed: 300m);
        Assert.Equal("implausible", Reason(fast));

        var normal = await Position(vehicle, at.AddSeconds(60), 0, 0.60m);
        Assert.Equal("Applied", Disposition(normal));

        // A phantom: a vehicle with no journey. Stored, never shown.
        var phantom = await Position("PHANTOM-9", at, 0, 0.5m);
        Assert.Equal("Ghost", Disposition(phantom));
        var ghost = await factory.WithDbAsync(db => db.TelemetryReadings.AsNoTracking().SingleAsync(r => r.VehicleCode == "PHANTOM-9"));
        Assert.Null(ghost.ResolvedJourneyId);

        using var client = factory.CreateClient();
        var board = await client.GetStringAsync($"/api/journeys/boards/{journey.Stops[0].Code}?count=50");
        Assert.DoesNotContain("PHANTOM-9", board);

        // Substitution: a spare takes over; its readings reach the same journey and the old vehicle's later readings do not.
        var swapAt = journey.Departure.AddMinutes(10);
        await factory.WithDbAsync(async db =>
        {
            var spare = Vehicle.Create("SPARE-1", "Spare trainset", VehicleKind.Trainset).Value;
            db.Vehicles.Add(spare);
            var entity = await db.Journeys.Include(j => j.Stops).Include(j => j.VehicleAssignments).SingleAsync(j => j.Id == journey.Id);
            entity.ReplaceVehicle(spare.Id, swapAt, "test", "fault");
            return await db.SaveChangesAsync();
        });

        var onSpare = await Position("SPARE-1", swapAt.AddMinutes(1), 0, 0.90m);
        Assert.Equal("Applied", Disposition(onSpare));
        Assert.Equal(journey.Id, onSpare.GetProperty("results")[0].GetProperty("journeyId").GetGuid());

        var oldVehicle = await Position(vehicle, swapAt.AddMinutes(2), 0, 0.95m);
        Assert.Equal("Ghost", Disposition(oldVehicle));
    }

    // ---- AC-19 ----

    [Fact]
    public async Task A_departure_a_delay_change_and_a_completion_are_each_raised_once()
    {
        var journey = await TakeJourneyAsync();
        using var feed = Feed(FeedKeys.Primary);
        var last = journey.Stops[^1];

        await SendEventsAsync(feed,
            Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure.AddMinutes(8)),
            Event(Seq(FeedKeys.Primary), journey.Id, 2, "arrived", journey.Stops[1].Arrival!.Value.AddMinutes(8)),      // same delay: no new event
            Event(Seq(FeedKeys.Primary), journey.Id, last.Sequence, "arrived", last.Arrival!.Value.AddMinutes(8)));      // still +8: completed

        Task<int> Count(string type) => factory.WithDbAsync(db => db.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM journeys.outbox_messages WHERE \"Type\" LIKE {"%" + type + "%"} AND \"Content\"::text LIKE {"%" + journey.Id + "%"}")
            .SingleAsync());

        Assert.Equal(1, await Count("JourneyDepartedDomainEvent"));
        Assert.Equal(1, await Count("JourneyDelayChangedDomainEvent"));
        Assert.Equal(1, await Count("JourneyCompletedDomainEvent"));
    }

    // ---- AC-20 ----

    [Fact]
    public async Task Two_sources_writing_to_the_same_journey_at_once_both_get_applied()
    {
        var journey = await TakeJourneyAsync();
        using var primary = Feed(FeedKeys.Primary);
        using var secondary = Feed(FeedKeys.Secondary);
        await SendEventsAsync(primary, Event(Seq(FeedKeys.Primary), journey.Id, 1, "departed", journey.Departure));

        var a = SendEventsAsync(primary, Event(Seq(FeedKeys.Primary), journey.Id, 2, "arrived", journey.Stops[1].Arrival!.Value.AddMinutes(1)));
        var b = SendEventsAsync(secondary, Event(Seq(FeedKeys.Secondary), journey.Id, 2, "departed", journey.Stops[1].Departure!.Value.AddMinutes(1)));
        var results = await Task.WhenAll(a, b);

        Assert.Equal("Applied", Disposition(results[0]));
        Assert.Equal("Applied", Disposition(results[1]));

        var stop = (await factory.WithDbAsync(db => db.Journeys.AsNoTracking().Include(j => j.Stops).SingleAsync(j => j.Id == journey.Id))).Stops.Single(s => s.Sequence == 2);
        Assert.NotNull(stop.ActualArrivalUtc);
        Assert.NotNull(stop.ActualDepartureUtc);
    }

    // ---- AC-1 (counted) and the rejected counter ----

    [Fact]
    public async Task Rejections_and_unauthorized_attempts_are_counted()
    {
        var journey = await TakeJourneyAsync();
        long rejected = 0, unauthorized = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name is "smartrail.ingest.rejected" or "smartrail.auth.failures")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "smartrail.ingest.rejected") Interlocked.Add(ref rejected, value);
            else Interlocked.Add(ref unauthorized, value);
        });
        listener.Start();

        using var bad = Feed((FeedKeys.Primary.Code, "wrong"));
        await bad.PostAsJsonAsync("/api/ingest/stop-events", new { events = new[] { Event(1, journey.Id, 1, "departed", journey.Departure) } });

        using var feed = Feed(FeedKeys.Primary);
        await SendEventsAsync(feed, Event(Seq(FeedKeys.Primary), Guid.NewGuid(), 1, "departed", journey.Departure));

        Assert.True(Interlocked.Read(ref unauthorized) >= 1);
        Assert.True(Interlocked.Read(ref rejected) >= 1);
    }
}

// 00:05 on 14 October, network time: the 23:50 journeys of 13 October are still running (REQ-LIVE-009).
public sealed class PostMidnightLiveTests(PostMidnightApiFactory factory) : IClassFixture<PostMidnightApiFactory>
{
    [Fact]
    public async Task A_running_journey_of_the_previous_service_date_shows_as_running_on_the_board_after_midnight()
    {
        using var warmUp = factory.CreateClient();
        var yesterday = new DateOnly(2026, 10, 13);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sp = scope.ServiceProvider;
            await sp.GetRequiredService<MediatR.ISender>().Send(new SmartRail.Modules.Journeys.Application.Generation.GenerateJourneysCommand(yesterday, 1));
            await sp.GetRequiredService<SmartRail.Modules.Journeys.Infrastructure.Seeding.DemoFleetSeeder>().SeedAsync([yesterday]);
        }

        var now = factory.Clock.GetUtcNow();
        var journey = await factory.WithDbAsync(db => db.Journeys.AsNoTracking().Include(j => j.Stops)
            .Where(j => j.ServiceDate == yesterday && j.ScheduledDepartureUtc < now && j.ScheduledArrivalUtc > now)
            .OrderBy(j => j.ScheduledDepartureUtc).FirstAsync());

        var stops = journey.Stops.OrderBy(s => s.Sequence).ToList();
        var next = stops.First(s => s.Sequence > 1 && s.Kind == JourneyStopKind.Stop && s.ScheduledDepartureUtc > now);

        using var feed = factory.CreateClient();
        feed.DefaultRequestHeaders.Add("X-Source-Code", FeedKeys.Primary.Code);
        feed.DefaultRequestHeaders.Add("X-Source-Key", FeedKeys.Primary.Key);
        using var response = await feed.PostAsJsonAsync("/api/ingest/stop-events", new
        {
            events = new[] { new { sequence = 1L, journeyId = journey.Id, stopSequence = 1, type = "departed", occurredAt = journey.ScheduledDepartureUtc.AddMinutes(2) } }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var board = await warmUp.GetFromJsonAsync<JsonElement>($"/api/journeys/boards/{next.StationCode}?count=50");
        var row = board.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("journeyId").GetGuid() == journey.Id);
        Assert.Equal("Running", row.GetProperty("status").GetString());
        Assert.Equal(2, row.GetProperty("delayMinutes").GetInt32());
        Assert.Equal("Live", row.GetProperty("estimateSource").GetString());
        Assert.True(row.GetProperty("expectedDepartureUtc").GetDateTimeOffset() >= next.ScheduledDepartureUtc);
    }
}
