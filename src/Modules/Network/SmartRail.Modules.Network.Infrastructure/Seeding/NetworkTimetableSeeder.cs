using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Timetables;
using SmartRail.Modules.Network.Infrastructure.Persistence;

namespace SmartRail.Modules.Network.Infrastructure.Seeding;

// Insert-only demo timetable (ASM-017). Calendars and the version are added only when missing and never changed after.
// Stop times are computed once here from each route's legs and stored explicitly.
public sealed class NetworkTimetableSeeder(
    NetworkDbContext context,
    TimeProvider clock,
    ILogger<NetworkTimetableSeeder> logger)
{
    private const string VersionLabel = "Demo timetable";
    private static readonly int[] WeekdayDepartures = [390, 480, 720, 1050];   // 06:30, 08:00, 12:00, 17:30

    private sealed record ServiceSeed(string TrainNumber, string RouteCode, ServiceDirection Direction, int Departure, string Calendar);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var calendarsAdded = await SeedCalendarsAsync(cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var servicesAdded = await SeedVersionAsync(cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Network timetable seed: added {Calendars} calendars and {Services} services (a version that already exists is left alone).",
            calendarsAdded, servicesAdded);
    }

    private async Task<int> SeedCalendarsAsync(CancellationToken ct)
    {
        var existing = (await context.ServiceCalendars.Select(c => c.Code).ToListAsync(ct)).ToHashSet();
        var start = new DateOnly(2026, 1, 1);
        var added = 0;

        foreach (var (code, name, days, exception) in new (string, string, DayOfWeek[], (DateOnly, CalendarExceptionKind, string)?)[]
                 {
                     ("WEEKDAYS", "Monday to Friday",
                         [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                         (new DateOnly(2026, 10, 23), CalendarExceptionKind.Removed, "Demo public holiday")),
                     ("DAILY", "Every day",
                         [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday],
                         null),
                     ("WEEKEND", "Saturday and Sunday",
                         [DayOfWeek.Saturday, DayOfWeek.Sunday],
                         (new DateOnly(2026, 10, 21), CalendarExceptionKind.Added, "Demo special event day"))
                 })
        {
            if (existing.Contains(code))
                continue;

            var calendar = ServiceCalendar.Create(code, name, days.ToHashSet(), start, null, SeedIds.For("calendar", code)).Value;
            if (exception is { } e)
                calendar.AddException(e.Item1, e.Item2, e.Item3);

            context.ServiceCalendars.Add(calendar);
            added++;
        }

        return added;
    }

    private async Task<int> SeedVersionAsync(CancellationToken ct)
    {
        if (await context.TimetableVersions.AnyAsync(v => v.Label == VersionLabel, ct))
            return 0;

        var routes = await context.Routes.Include(r => r.Stops).ToDictionaryAsync(r => r.Code, ct);
        var calendars = await context.ServiceCalendars.ToDictionaryAsync(c => c.Code, c => c.Id, ct);

        var version = TimetableVersion.CreateDraft(VersionLabel, "seed", SeedIds.For("version", VersionLabel)).Value;

        foreach (var seed in BuildSeeds())
        {
            var route = routes[seed.RouteCode];
            var service = Service.Create(
                version.Id,
                route,
                calendars[seed.Calendar],
                seed.TrainNumber,
                seed.Direction,
                ServiceCategory.Regional,
                BuildStops(route, seed.Direction, seed.Departure),
                SeedIds.For("service", seed.TrainNumber)).Value;

            version.AddService(service);
        }

        version.SubmitForReview();
        version.Publish(new DateOnly(2026, 1, 1), "seed-approver", clock.GetUtcNow());

        context.TimetableVersions.Add(version);
        return version.Services.Count;
    }

    // 32 weekday services (4 routes x 2 directions x 4 departures), 3 daily (one ends after midnight), 2 at weekends.
    private static IEnumerable<ServiceSeed> BuildSeeds()
    {
        var bases = new Dictionary<string, int> { ["MAIN"] = 1000, ["HILL"] = 2000, ["COAST"] = 3000, ["NORTH"] = 4000 };

        foreach (var (route, baseNumber) in bases)
        {
            for (var i = 0; i < WeekdayDepartures.Length; i++)
            {
                yield return new ServiceSeed((baseNumber + 1 + i).ToString(), route, ServiceDirection.Forward, WeekdayDepartures[i], "WEEKDAYS");
                yield return new ServiceSeed((baseNumber + 501 + i).ToString(), route, ServiceDirection.Reverse, WeekdayDepartures[i], "WEEKDAYS");
            }
        }

        yield return new ServiceSeed("D101", "MAIN", ServiceDirection.Forward, 1200, "DAILY");   // 20:00
        yield return new ServiceSeed("D102", "MAIN", ServiceDirection.Reverse, 1200, "DAILY");
        yield return new ServiceSeed("D201", "HILL", ServiceDirection.Forward, 1430, "DAILY");   // 23:50, ends after midnight

        yield return new ServiceSeed("W301", "COAST", ServiceDirection.Forward, 600, "WEEKEND"); // 10:00
        yield return new ServiceSeed("W302", "COAST", ServiceDirection.Reverse, 600, "WEEKEND");
    }

    // Times follow the route's legs; in reverse the leg between two stops is the same as going forward.
    private static List<ServiceStopDraft> BuildStops(Route route, ServiceDirection direction, int departure)
    {
        var forward = route.Stops.OrderBy(s => s.Sequence).ToList();
        var ordered = direction == ServiceDirection.Forward ? forward : Enumerable.Reverse(forward).ToList();

        var drafts = new List<ServiceStopDraft>();
        var clock = departure;
        var last = ordered.Count - 1;

        for (var j = 0; j <= last; j++)
        {
            var stop = ordered[j];

            if (j == 0)
            {
                drafts.Add(new ServiceStopDraft(1, stop.StationId, null, departure, stop.DefaultPlatformId, stop.Kind));
                continue;
            }

            var leg = direction == ServiceDirection.Forward ? stop.MinutesFromPrevious : ordered[j - 1].MinutesFromPrevious;
            var arrival = clock + leg;
            int? leave = j == last ? null : arrival + stop.DwellMinutes;
            clock = leave ?? arrival;

            drafts.Add(new ServiceStopDraft(j + 1, stop.StationId, arrival, leave, stop.DefaultPlatformId, stop.Kind));
        }

        return drafts;
    }

    private static class SeedIds
    {
        internal static Guid For(string kind, string key) =>
            new(MD5.HashData(Encoding.UTF8.GetBytes($"smartrail:network:{kind}:{key}")));
    }
}
