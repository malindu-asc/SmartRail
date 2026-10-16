using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Domain.Timetables;

namespace SmartRail.Modules.Network.Infrastructure.Persistence;

internal sealed class TimetableReader(NetworkDbContext context) : ITimetableReader
{
    public async Task<TimetableDayReadModel?> GetDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        // 1. Which version is in force on this date?
        var summaries = await context.TimetableVersions
            .AsNoTracking()
            .Select(v => new TimetableVersionSummary(v.Id, v.Status, v.EffectiveFrom))
            .ToListAsync(cancellationToken);

        var version = VersionResolver.Resolve(summaries, date);
        if (version is null)
            return null;

        // 2. Which calendars run on this date (weekday, valid range, exceptions)?
        var calendars = await context.ServiceCalendars
            .AsNoTracking()
            .Include(c => c.Exceptions)
            .ToListAsync(cancellationToken);
        var running = calendars.Where(c => c.RunsOn(date)).Select(c => c.Id).ToList();

        // 3. The services of that version on those calendars, with their stops.
        var services = await context.Services
            .AsNoTracking()
            .Include(s => s.Stops)
            .AsSplitQuery()
            .Where(s => s.TimetableVersionId == version.Id && running.Contains(s.CalendarId))
            .ToListAsync(cancellationToken);

        var routes = await context.Routes.AsNoTracking()
            .Where(r => r.IsActive)
            .ToDictionaryAsync(r => r.Id, r => r.Code, cancellationToken);

        var stations = await context.Stations.AsNoTracking()
            .Include(s => s.Platforms)
            .Include(s => s.Translations)
            .AsSplitQuery()
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var models = services
            .Where(s => routes.ContainsKey(s.RouteId))
            .OrderBy(s => s.TrainNumber, StringComparer.Ordinal)
            .Select(s => new ServiceReadModel(
                s.Id,
                s.TrainNumber,
                routes[s.RouteId],
                s.Stops.OrderBy(st => st.Sequence).Select(st =>
                {
                    var station = stations[st.StationId];
                    return new ServiceStopReadModel(
                        st.Sequence,
                        station.Code,
                        station.Name,
                        station.Translations.ToDictionary(t => t.LanguageCode, t => t.Name, StringComparer.OrdinalIgnoreCase),
                        st.ArrivalMinutes,
                        st.DepartureMinutes,
                        st.DefaultPlatformId is { } platformId
                            ? station.Platforms.FirstOrDefault(p => p.Id == platformId)?.Code
                            : null,
                        st.Kind.ToString());
                }).ToList()))
            .ToList();

        return new TimetableDayReadModel(version.Id, models);
    }

    public Task<bool> ServiceExistsAsync(Guid serviceId, CancellationToken cancellationToken) =>
        context.Services.AsNoTracking().AnyAsync(s => s.Id == serviceId, cancellationToken);
}
