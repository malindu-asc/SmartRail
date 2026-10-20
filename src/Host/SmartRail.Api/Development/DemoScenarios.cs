using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Identity.Domain.Devices;
using SmartRail.Modules.Identity.Infrastructure.Persistence;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Fleet;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Journeys.Infrastructure.Persistence;
using SmartRail.Modules.Network.Contracts;
using SmartRail.Modules.Operations.Domain.Disruptions;
using SmartRail.Modules.Operations.Domain.Incidents;
using SmartRail.Modules.Operations.Domain.Messages;
using SmartRail.Modules.Operations.Infrastructure.Persistence;
using SmartRail.Modules.Ticketing.Domain.Bookings;
using SmartRail.Modules.Ticketing.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace SmartRail.Api.Development;

// Development scenarios (flag Demo:Scenarios:Enabled, off by default). Puts today's timetable into the states the passenger and driver
// apps have to handle, through the same domain methods the live feed uses, so every screen reads real rows:
//   MAIN  : a finished train that ran early, one that ran on time, a running train on time, a platform change, a spare train.
//   HILL  : a finished train that ran late, a running train 12 minutes late, a cancelled train, a spare train.
//   COAST : a running train that has not been heard from for ten minutes (no live data).
//   NORTH : a running train 4 minutes late, a seat-reserved train ahead, a platform change.
// Tickets for the demo passenger sit on those trains (valid, delayed, affected, past) and one is left unattached to try "Add ticket".
// Insert-only and safe to run again: it only touches trains that are still Ready.
public sealed class DemoScenarioSeeder(
    JourneysDbContext journeysDb,
    TicketingDbContext ticketingDb,
    OperationsDbContext operationsDb,
    IdentityDbContext identityDb,
    INetworkTimeZone timeZone,
    TimeProvider clock,
    ILogger<DemoScenarioSeeder> logger)
{
    // The demo passenger of the Keycloak realm.
    public const string PassengerSubject = "a1000000-0000-4000-8000-000000000001";
    public const string GuestBookingCode = "DEMO2468";

    // The demo driver of the Keycloak realm and the train they drive.
    public const string DriverSubject = "a1000000-0000-4000-8000-000000000002";
    public const string DriverTrain = "F102";

    // The demo staff of the Keycloak realm (see infra/keycloak/smartrail-realm.json).
    public const string ControllerSubject = "a1000000-0000-4000-8000-000000000003";
    public const string SecondControllerSubject = "a1000000-0000-4000-8000-000000000006";
    public const string SchedulerSubject = "a1000000-0000-4000-8000-000000000005";

    private const string Actor = "demo-scenarios";
    private static readonly LiveRules Rules = LiveRules.Default;

    private static readonly Dictionary<string, string> ForwardOrigin = new()
    {
        ["MAIN"] = "CEN",
        ["HILL"] = "CEN",
        ["COAST"] = "CEN",
        ["NORTH"] = "MAR"
    };

    public async Task<ScenarioResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var today = timeZone.Today();

        var all = await journeysDb.Journeys
            .Include(j => j.Stops)
            .Where(j => j.ServiceDate == today)
            .OrderBy(j => j.ScheduledDepartureUtc)
            .ToListAsync(cancellationToken);

        // The timetable is sparse, so trains of our own are added around "now": some already on their way, some about to leave.
        var added = await EnsureDemoTrainsAsync(all, today, now, cancellationToken);
        if (added)
            all = all.OrderBy(j => j.ScheduledDepartureUtc).ToList();

        List<Journey> Chain(string route) => all
            .Where(j => j.RouteCode == route && j.OriginStationCode == ForwardOrigin[route] && j.Lifecycle == JourneyLifecycle.Ready)
            .ToList();

        var main = Chain("MAIN");
        var hill = Chain("HILL");
        var coast = Chain("COAST");
        var north = Chain("NORTH");

        var applied = 0;

        // ---- trains ----
        var mainPast = main.Where(j => j.ScheduledArrivalUtc <= now).ToList();
        var mainLive = main.FirstOrDefault(j => j.ScheduledDepartureUtc <= now && j.ScheduledArrivalUtc > now);
        var mainFuture = main.Where(j => j.ScheduledDepartureUtc > now).ToList();

        var hillPast = hill.Where(j => j.ScheduledArrivalUtc <= now).ToList();
        var hillLive = hill.FirstOrDefault(j => j.ScheduledDepartureUtc <= now && j.ScheduledArrivalUtc > now);
        var hillFuture = hill.Where(j => j.ScheduledDepartureUtc > now).ToList();

        var coastLive = coast.FirstOrDefault(j => j.ScheduledDepartureUtc <= now && j.ScheduledArrivalUtc > now);
        var northLive = north.FirstOrDefault(j => j.ScheduledDepartureUtc <= now && j.ScheduledArrivalUtc > now);
        var northFuture = north.Where(j => j.ScheduledDepartureUtc > now).ToList();

        // Finished trains: the latest one early (3 minutes), the one before on time.
        if (mainPast.Count > 0) applied += Run(mainPast[^1], -3, now, now);
        if (mainPast.Count > 1) applied += Run(mainPast[^2], 0, now, now);
        if (hillPast.Count > 0) applied += Run(hillPast[^1], 6, now, now);

        // Running trains.
        if (mainLive is not null) applied += Run(mainLive, 0, now, now);
        if (hillLive is not null) applied += Run(hillLive, 12, now, now);
        if (northLive is not null) applied += Run(northLive, 4, now, now);
        // Not heard from for ten minutes: the passenger sees "No live data" and the scheduled times only.
        if (coastLive is not null) applied += Run(coastLive, 0, now, now.AddMinutes(-10));

        // The other direction and the rest of the day.
        Journey? By(string number) => all.FirstOrDefault(j => j.TrainNumber == number && j.Lifecycle == JourneyLifecycle.Ready);
        if (By("R101") is { } r101 && r101.ScheduledDepartureUtc <= now) applied += Run(r101, 5, now, now);
        if (By("R201") is { } r201 && r201.ScheduledDepartureUtc <= now) applied += Run(r201, 15, now, now);
        if (By("R301") is { } r301 && r301.ScheduledDepartureUtc <= now) applied += Run(r301, 0, now, now);
        if (By("R401") is { } r401 && r401.ScheduledDepartureUtc <= now) applied += Run(r401, -2, now, now);
        var f302 = By("F302");
        if (f302 is not null && f302.Cancel("operational").IsSuccess) applied++;
        if (By("F301") is { } f301 && ChangePlatform(f301, now)) applied++;

        // Cancelled and platform changes.
        if (hillFuture.Count > 0 && hillFuture[0].Cancel("operational").IsSuccess) applied++;
        if (mainFuture.Count > 0 && ChangePlatform(mainFuture[0], now)) applied++;
        if (northFuture.Count > 0 && ChangePlatform(northFuture[0], now)) applied++;

        await journeysDb.SaveChangesAsync(cancellationToken);

        // ---- tickets, on the trains chosen above ----
        var tickets = 0;
        tickets += await AddBookingAsync("past", mainPast.Count > 0 ? mainPast[^1] : null, "CEN", "OAK", 1, 0, 0, null, false, PassengerSubject, now, cancellationToken);
        tickets += await AddBookingAsync("valid", mainFuture.Count > 0 ? mainFuture[0] : null, "CEN", "RIV", 1, 1, 0, null, false, PassengerSubject, now, cancellationToken);
        tickets += await AddBookingAsync("seat", northFuture.Count > 0 ? northFuture[0] : null, "MAR", "AIR", 2, 0, 0, [new SeatRef("B", 13), new SeatRef("B", 14)], true, PassengerSubject, now, cancellationToken);
        tickets += await AddBookingAsync("delayed", hillLive, "CEN", "MAR", 1, 0, 0, null, false, PassengerSubject, now, cancellationToken);
        tickets += await AddBookingAsync("affected", hillFuture.Count > 0 ? hillFuture[0] : null, "CEN", "HIL", 1, 0, 1, null, false, PassengerSubject, now, cancellationToken);
        tickets += await AddBookingAsync("guest", mainFuture.Count > 1 ? mainFuture[1] : null, "CEN", "OAK", 1, 0, 0, null, false, null, now, cancellationToken);

        tickets += await AddBookingAsync("family", By("F103"), "CEN", "RIV", 1, 1, 1, null, false, PassengerSubject, now, cancellationToken);
        tickets += await AddBookingAsync("reverse", By("R101") ?? all.FirstOrDefault(j => j.TrainNumber == "R101"), "RIV", "OAK", 1, 0, 0, null, false, PassengerSubject, now, cancellationToken);
        tickets += await AddBookingAsync("coast", By("F301"), "CEN", "BAY", 2, 0, 0, null, false, PassengerSubject, now, cancellationToken);
        tickets += await AddBookingAsync("affected2", f302 ?? all.FirstOrDefault(j => j.TrainNumber == "F302"), "BAY", "CEN", 1, 0, 0, null, false, PassengerSubject, now, cancellationToken);

        // ---- notices ----
        var notices = await AddDisruptionsAsync(now, cancellationToken);
        notices += await AddOpsAsync(now, cancellationToken);

        logger.LogInformation("Demo scenarios: {Trains} train states, {Tickets} tickets, {Notices} notices", applied, tickets, notices);
        return new ScenarioResult(applied, tickets, notices);
    }

    // L = running now, R = running now the other way, F = still to leave. Reverse trains run from the route's far end.
    private static readonly (string Number, string Route, int MinutesFromNow, bool Forward)[] DemoTrains =
    [
        ("L101", "MAIN", -12, true), ("L201", "HILL", -14, true), ("L301", "COAST", -12, true), ("L401", "NORTH", -9, true),
        ("R101", "MAIN", -10, false), ("R201", "HILL", -16, false), ("R301", "COAST", -11, false), ("R401", "NORTH", -8, false),
        ("F101", "MAIN", 25, true), ("F102", "MAIN", 55, true), ("F103", "MAIN", 85, true), ("F201", "HILL", 40, true),
        ("F202", "HILL", 70, true), ("F301", "COAST", 35, true), ("F302", "COAST", 60, false), ("F401", "NORTH", 30, true),
        ("F402", "NORTH", 50, false), ("F403", "NORTH", 90, true)
    ];

    // A copy of the route's first train, moved so it leaves at the wanted time, with a vehicle and a driver so it is Ready.
    private async Task<bool> EnsureDemoTrainsAsync(List<Journey> all, DateOnly today, DateTimeOffset now, CancellationToken ct)
    {
        var vehicles = await journeysDb.Vehicles.ToDictionaryAsync(v => v.Code, ct);
        var any = false;
        var trainsets = 0;

        foreach (var (number, route, minutes, forward) in DemoTrains)
        {
            if (all.Any(j => j.TrainNumber == number))
                continue;

            var template = all.FirstOrDefault(j => j.RouteCode == route && (j.OriginStationCode == ForwardOrigin[route]) == forward);
            if (template is null)
                continue;

            var wanted = now.AddMinutes(minutes);
            var delta = new DateTimeOffset(wanted.Year, wanted.Month, wanted.Day, wanted.Hour, wanted.Minute, 0, TimeSpan.Zero) - template.ScheduledDepartureUtc;

            var drafts = template.Stops.OrderBy(s => s.Sequence)
                .Select(s => new JourneyStopDraft(
                    s.Sequence, s.StationCode, s.Kind, s.ScheduledArrivalUtc + delta, s.ScheduledDepartureUtc + delta,
                    s.DwellMinutes, s.PlatformOriginalCode))
                .ToList();

            var created = Journey.Create(null, null, today, number, route, JourneyCreationOrigin.Manual,
                "Development scenario train", drafts, StableId($"train:{number}:{today:yyyy-MM-dd}"));
            if (created.IsFailure)
                continue;

            var journey = created.Value;
            // F102 is driven by the demo driver, so the driver app has a train to start, run and finish.
            var driver = number == DriverTrain ? DriverSubject : $"seed-driver-scenario-{number.ToLowerInvariant()}";
            var from = journey.ScheduledDepartureUtc.AddHours(-1);
            var to = journey.ScheduledArrivalUtc.AddHours(1);

            // A driver cannot hold two overlapping duties (the fleet seeder may already have rostered the demo driver).
            if (number == DriverTrain && await journeysDb.CrewDuties.AnyAsync(d => d.DriverSubject == DriverSubject && d.RosteredFrom < to && d.RosteredTo > from, ct))
                driver = $"seed-driver-scenario-{number.ToLowerInvariant()}";

            var duty = CrewDuty.Create(driver, today,
                from, to,
                StableId($"duty:{number}:{today:yyyy-MM-dd}"));
            if (duty.IsFailure)
                continue;

            // A trainset of its own: the regular one is already on the line with the timetable's trains.
            var code = $"DEMO-{number}";
            if (!vehicles.TryGetValue(code, out var vehicle))
            {
                vehicle = Vehicle.Create(code, $"Demo trainset {number}", VehicleKind.Trainset, StableId($"vehicle:{code}")).Value;
                journeysDb.Vehicles.Add(vehicle);
                vehicles[code] = vehicle;
                trainsets++;
            }

            journeysDb.CrewDuties.Add(duty.Value);
            journey.AssignVehicle(vehicle.Id, journey.ScheduledDepartureUtc, journey.ScheduledArrivalUtc, Actor);
            journey.AssignDriver(duty.Value.Id, journey.ScheduledDepartureUtc, journey.ScheduledArrivalUtc, Actor);

            journeysDb.Journeys.Add(journey);
            all.Add(journey);
            any = true;
        }

        if (any)
            await journeysDb.SaveChangesAsync(ct);

        return any;
    }

    // The train as it would have run with this delay, up to the time the feed was last heard from.
    private int Run(Journey journey, int delayMinutes, DateTimeOffset now, DateTimeOffset heardAt)
    {
        var delay = TimeSpan.FromMinutes(delayMinutes);
        var stops = journey.Stops.OrderBy(s => s.Sequence).ToList();
        var first = stops[0].Sequence;
        var last = stops[^1].Sequence;
        var any = false;

        bool Apply(StopEventType type, int sequence, DateTimeOffset? scheduled)
        {
            if (scheduled is null)
                return true;

            var at = scheduled.Value + delay;
            if (at > heardAt)
                return false;

            any |= journey.ApplyStopEvent(type, sequence, at, heardAt, Rules).IsSuccess;
            return true;
        }

        foreach (var stop in stops)
        {
            if (stop.Kind == JourneyStopKind.Passing)
            {
                if (!Apply(StopEventType.Passed, stop.Sequence, stop.ScheduledDepartureUtc ?? stop.ScheduledArrivalUtc))
                    break;
                continue;
            }

            if (stop.Sequence != first && !Apply(StopEventType.Arrived, stop.Sequence, stop.ScheduledArrivalUtc))
                break;

            if (stop.Sequence != last && !Apply(StopEventType.Departed, stop.Sequence, stop.ScheduledDepartureUtc))
                break;
        }

        return any ? 1 : 0;
    }

    // The platform at the second stop changes, so Departures and the journey show "Platform changed".
    private bool ChangePlatform(Journey journey, DateTimeOffset now)
    {
        var stop = journey.Stops.OrderBy(s => s.Sequence).Skip(1).FirstOrDefault(s => s.Kind != JourneyStopKind.Passing);
        if (stop?.PlatformCode is null)
            return false;

        var next = stop.PlatformCode == "1" ? "2" : "1";
        return journey.ChangePlatform(stop.Sequence, next, Actor, now).IsSuccess;
    }

    private async Task<int> AddBookingAsync(
        string key, Journey? journey, string from, string to, int adults, int children, int seniors,
        IReadOnlyList<SeatRef>? seats, bool needsHelp, string? userId, DateTimeOffset now, CancellationToken ct)
    {
        if (journey is null)
            return 0;

        var id = StableId($"booking:{key}:{userId ?? "guest"}");
        if (await ticketingDb.Bookings.AnyAsync(b => b.Id == id, ct))
            return 0;

        var stops = journey.Stops.OrderBy(s => s.Sequence).ToList();
        var fromIndex = stops.FindIndex(s => s.StationCode == from);
        var toIndex = stops.FindIndex(s => s.StationCode == to);
        if (fromIndex < 0 || toIndex <= fromIndex)
            return 0;

        var departure = stops[fromIndex].ScheduledDepartureUtc ?? journey.ScheduledDepartureUtc;
        var arrival = stops[toIndex].ScheduledArrivalUtc ?? journey.ScheduledArrivalUtc;
        var fare = FareCalculator.Quote(new FareOptions(), (int)(arrival - departure).TotalMinutes, adults, children, seniors);

        var reference = $"SR-{Digits(key, 0)}-{Digits(key, 4)}";
        var code = key == "guest" ? GuestBookingCode : Code(key);

        if (await ticketingDb.Bookings.AnyAsync(b => b.Reference == reference || b.BookingCode == code, ct))
            return 0;

        ticketingDb.Bookings.Add(new Booking(
            id, reference, code, userId, userId is null ? "claim.me@example.test" : null,
            journey.Id, journey.ServiceDate, journey.RouteCode, journey.TrainNumber,
            from, to, fromIndex, toIndex, departure, arrival,
            adults, children, seniors, needsHelp,
            fare.Total, fare.Currency, seats ?? [], now));

        await ticketingDb.SaveChangesAsync(ct);
        return 1;
    }

    private async Task<int> AddDisruptionsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var added = 0;

        async Task AddAsync(string key, string title, string message, DisruptionSeverity severity, bool networkWide, string[] stations, int endsInMinutes, bool resolved = false)
        {
            var id = StableId($"disruption:{key}");
            if (await operationsDb.Disruptions.AnyAsync(d => d.Id == id, ct))
                return;

            var created = Disruption.Create(
                title, message, severity, networkWide, stations,
                now.AddMinutes(endsInMinutes), now.AddHours(4), ControllerSubject, now, id, type: "Demo");
            if (created.IsFailure)
                return;

            // A network-wide notice needs a second person; the demo's second controller approves it.
            if (networkWide)
                created.Value.Approve(SecondControllerSubject, now);

            if (resolved)
                created.Value.Resolve(now);

            operationsDb.Disruptions.Add(created.Value);
            added++;
        }

        await AddAsync("mill-lane", "Signal fault at Mill Lane",
            "A signal fault is slowing trains through Mill Lane. Expect delays of up to 15 minutes while it is repaired.",
            DisruptionSeverity.Major, false, ["MIL"], 90);
        await AddAsync("hill-lift", "Lift out of service at Hill Town",
            "The lift to platform 2 is out of service. Step-free access is available on platform 1.",
            DisruptionSeverity.Minor, false, ["HIL"], 240);
        await AddAsync("harbour-works", "Harbour line closed for track works",
            "Trains do not call at Harbour while the track is replaced. Use the Coast line services from Central to Lakeside.",
            DisruptionSeverity.Severe, false, ["HAR", "LAK"], 300);
        await AddAsync("market-crowding", "Busy at Market Street",
            "Expect crowding on the evening trains. Allow a few extra minutes to reach your platform.",
            DisruptionSeverity.Minor, false, ["MAR"], 120);
        await AddAsync("airport-lift", "Airport lift repaired",
            "The lift at Airport is working again.",
            DisruptionSeverity.Minor, false, ["AIR"], 5, resolved: true);
        await AddAsync("network-check", "Planned track inspection",
            "Some evening services may run a few minutes late while track inspections take place across the network.",
            DisruptionSeverity.Minor, true, [], 360);

        await operationsDb.SaveChangesAsync(ct);
        return added;
    }

    // What the controller, scheduler and administrator consoles show on a fresh database: a notice waiting for approval, planned works,
    // an open and a closed incident, driver messages (one not acknowledged yet), an assistance request and the device register.
    private async Task<int> AddOpsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var added = 0;

        // ---- notices ----
        var waitingId = StableId("disruption:approval-pending");
        if (!await operationsDb.Disruptions.AnyAsync(d => d.Id == waitingId, ct))
        {
            var waiting = Disruption.Create("Reduced service on all lines tomorrow morning",
                "Fewer trains run before 07:00 tomorrow while crews change shifts. Check your train before you travel.",
                DisruptionSeverity.Major, true, [], now.AddHours(12), now.AddHours(6), SecondControllerSubject, now, waitingId, "Staffing");
            if (waiting.IsSuccess)
            {
                operationsDb.Disruptions.Add(waiting.Value);
                added++;
            }
        }

        var plannedId = StableId("disruption:planned-works");
        if (!await operationsDb.Disruptions.AnyAsync(d => d.Id == plannedId, ct))
        {
            var start = now.AddDays(2);
            var planned = Disruption.Create("Track renewal between Central and Mill Lane",
                "No trains between Central and Mill Lane from 22:00. Buses replace trains.",
                DisruptionSeverity.Severe, false, ["CEN", "MIL"], start.AddHours(8), start.AddHours(9), SchedulerSubject, now, plannedId, "Planned works", start);
            if (planned.IsSuccess)
            {
                operationsDb.Disruptions.Add(planned.Value);
                added++;
            }
        }

        var draftId = StableId("disruption:draft");
        if (!await operationsDb.Disruptions.AnyAsync(d => d.Id == draftId, ct))
        {
            var draft = Disruption.Create("Lift works at Oak Park", "The lift at Oak Park will be out of service for a day.",
                DisruptionSeverity.Minor, false, ["OAK"], null, now.AddHours(24), ControllerSubject, now, draftId, "Facilities", submit: false);
            if (draft.IsSuccess)
            {
                operationsDb.Disruptions.Add(draft.Value);
                added++;
            }
        }

        // ---- incidents ----
        var openId = StableId("incident:open");
        if (!await operationsDb.Incidents.AnyAsync(i => i.Id == openId, ct))
        {
            var open = Incident.Declare("Signal failure at Mill Lane", IncidentSeverity.Major, ControllerSubject, "Reported by the signaller", ["MIL"],
                StableId("disruption:mill-lane"), now.AddMinutes(-35), openId);
            if (open.IsSuccess)
            {
                open.Value.AddNote(IncidentEntryKind.Action, "Told the drivers of trains 1004 and 1006 to hold at the previous station", null, ControllerSubject, now.AddMinutes(-30));
                open.Value.AddNote(IncidentEntryKind.Note, "Signalling team on site, estimated repair in one hour", null, ControllerSubject, now.AddMinutes(-20));
                operationsDb.Incidents.Add(open.Value);
                added++;
            }
        }

        var closedId = StableId("incident:closed");
        if (!await operationsDb.Incidents.AnyAsync(i => i.Id == closedId, ct))
        {
            var closed = Incident.Declare("Door fault on a Coast line train", IncidentSeverity.Minor, SecondControllerSubject, "Reported by the driver", ["BAY"], null, now.AddHours(-6), closedId);
            if (closed.IsSuccess)
            {
                closed.Value.Resolve("Train swapped at Bay Side; the faulty unit is in the workshop", SecondControllerSubject, now.AddHours(-5));
                operationsDb.Incidents.Add(closed.Value);
                added++;
            }
        }

        // ---- driver messages and assistance ----
        var waitingMessageId = StableId("message:hold");
        if (!await operationsDb.ControlMessages.AnyAsync(m => m.Id == waitingMessageId, ct))
        {
            var hold = ControlMessage.Send(DriverSubject, null, DriverTrain, null, openId, "hold", "Hold at the next station until the signaller clears Mill Lane.",
                true, now.AddHours(2), ControllerSubject, now.AddMinutes(-8), waitingMessageId);
            if (hold.IsSuccess)
            {
                operationsDb.ControlMessages.Add(hold.Value);
                added++;
            }
        }

        var doneMessageId = StableId("message:thanks");
        if (!await operationsDb.ControlMessages.AnyAsync(m => m.Id == doneMessageId, ct))
        {
            var done = ControlMessage.Send(DriverSubject, null, DriverTrain, null, null, null, "Platform 2 is free now. Use platform 2 at Central.",
                true, now.AddHours(2), ControllerSubject, now.AddMinutes(-50), doneMessageId);
            if (done.IsSuccess)
            {
                done.Value.MarkDelivered(now.AddMinutes(-49));
                done.Value.Acknowledge(now.AddMinutes(-48));
                operationsDb.ControlMessages.Add(done.Value);
                added++;
            }
        }

        var helpId = StableId("assistance:open");
        if (!await operationsDb.AssistanceRequests.AnyAsync(a => a.Id == helpId, ct))
        {
            operationsDb.AssistanceRequests.Add(AssistanceRequest.Create(DriverSubject, null, DriverTrain, "MIL", "Passenger unwell on board, please arrange help at the station", now.AddMinutes(-4), helpId));
            added++;
        }

        await operationsDb.SaveChangesAsync(ct);

        // ---- the device register (displays, gates, terminals) with how recently each was heard from ----
        if (!await identityDb.Devices.AnyAsync(ct))
        {
            var devices = DemoDevices;

            foreach (var d in devices)
            {
                var device = Device.Register(d.Name, d.Type, d.Station, d.Location, "demo-scenarios", now.AddDays(-30), StableId($"device:{d.Name}"));
                if (device.IsFailure)
                    continue;

                if (d.HeardSecondsAgo is { } ago)
                    device.Value.Heartbeat(now.AddSeconds(-ago), "1.4.2");

                identityDb.Devices.Add(device.Value);
                added++;
            }

            await identityDb.SaveChangesAsync(ct);
        }

        return added;
    }

    internal static readonly (string Name, DeviceType Type, string Station, string Location, int? HeardSecondsAgo)[] DemoDevices =
    [
        ("Central platform 1 display", DeviceType.PlatformDisplay, "CEN", "Platform 1", 12),
        ("Central platform 2 display", DeviceType.PlatformDisplay, "CEN", "Platform 2", 20),
        ("Central concourse board", DeviceType.ConcourseBoard, "CEN", "Main concourse", 8),
        ("Mill Lane platform display", DeviceType.PlatformDisplay, "MIL", "Platform 1", 75),
        ("Hill Town platform display", DeviceType.PlatformDisplay, "HIL", "Platform 1", 400),
        ("Central gate 3", DeviceType.GateScanner, "CEN", "Gate 3", 15),
        ("Market Street ticket terminal", DeviceType.Terminal, "MAR", "Ticket hall", 30),
        ("Airport platform display", DeviceType.PlatformDisplay, "AIR", "Platform 1", null)
    ];

    private static Guid StableId(string key) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes($"smartrail:demo-scenarios:{key}")));

    private static string Digits(string key, int offset)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"smartrail:demo-ref:{key}"));
        return (BitConverter.ToUInt16(hash, offset / 2) % 10000).ToString("D4");
    }

    private static string Code(string key)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"smartrail:demo-code:{key}"));
        return new string(hash.Take(8).Select(b => alphabet[b % alphabet.Length]).ToArray());
    }
}

public sealed record ScenarioResult(int Trains, int Tickets, int Notices);

// Applies the scenarios once the day's trains exist, then keeps the running demo trains "heard from" (their last update moves forward
// every 20 seconds) so they stay Live instead of ageing into Stale and No live data while the apps are being tried. Trains that have
// real events (the driver app, the feed) and trains that were left silent on purpose are not touched.
public sealed class DemoScenarioWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<DemoScenarioWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var applied = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();

                if (!applied)
                {
                    var journeys = scope.ServiceProvider.GetRequiredService<JourneysDbContext>();
                    var today = scope.ServiceProvider.GetRequiredService<INetworkTimeZone>().Today();
                    if (await journeys.Journeys.AnyAsync(j => j.ServiceDate == today && j.Lifecycle == JourneyLifecycle.Ready, stoppingToken))
                    {
                        await scope.ServiceProvider.GetRequiredService<DemoScenarioSeeder>().ApplyAsync(stoppingToken);
                        applied = true;
                    }
                }
                else
                {
                    await KeepAliveAsync(scope.ServiceProvider, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Demo scenarios: step failed, trying again");
            }

            await Task.Delay(applied ? TimeSpan.FromSeconds(20) : TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task KeepAliveAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<JourneysDbContext>();
        var now = clock.GetUtcNow();
        var recent = now.AddMinutes(-4);

        var running = await db.Journeys
            .Where(j => j.Lifecycle == JourneyLifecycle.Running
                && j.LastAcceptedUpdateAt > recent
                && !db.JourneyEvents.Any(e => e.JourneyId == j.Id))
            .ToListAsync(ct);

        foreach (var journey in running)
            journey.RecordPosition(now);

        if (running.Count > 0)
            await db.SaveChangesAsync(ct);

        await KeepDevicesAliveAsync(services.GetRequiredService<IdentityDbContext>(), now, ct);
    }

    // The register has no heartbeat intake (devices do not sign in here), so the demo's own devices are "heard from" again every
    // 20 seconds with the same age they were seeded with: some Online, one Late, one Offline, one never heard from.
    private static async Task KeepDevicesAliveAsync(IdentityDbContext identity, DateTimeOffset now, CancellationToken ct)
    {
        var names = DemoScenarioSeeder.DemoDevices.Where(d => d.HeardSecondsAgo is not null).ToDictionary(d => d.Name, d => d.HeardSecondsAgo!.Value);
        var devices = await identity.Devices.Where(d => names.Keys.Contains(d.Name) && d.Status != DeviceStatus.Revoked).ToListAsync(ct);
        foreach (var device in devices)
            device.Heartbeat(now.AddSeconds(-names[device.Name]), "1.4.2");

        if (devices.Count > 0)
            await identity.SaveChangesAsync(ct);
    }
}
