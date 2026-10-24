using Microsoft.EntityFrameworkCore;
using SmartRail.Infrastructure.Core.Audit;
using SmartRail.Modules.Network.Application.Timetables.Versions;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Timetables;

namespace SmartRail.Modules.Network.Infrastructure.Persistence;

internal sealed class NetworkAuditLog(NetworkDbContext context, TimeProvider clock)
    : AuditLogWriter(context, clock), INetworkAuditLog;

internal sealed class TimetableVersionStore(NetworkDbContext context) : ITimetableVersionStore
{
    public async Task<IReadOnlyList<VersionListItem>> ListAsync(CancellationToken cancellationToken) =>
        await context.TimetableVersions.AsNoTracking()
            .Select(v => new VersionListItem(v.Id, v.Label, v.Status.ToString(), v.EffectiveFrom, v.Author, v.ApprovedBy, v.Services.Count, v.CreatedAtUtc, v.PublishedAt))
            .ToListAsync(cancellationToken);

    public Task<TimetableVersion?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.TimetableVersions.Include(v => v.Services).ThenInclude(s => s.Stops).FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<TimetableVersion?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken) =>
        context.TimetableVersions.AsNoTracking().AsSplitQuery().Include(v => v.Services).ThenInclude(s => s.Stops)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<TimetableVersion?> GetInForceAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var summaries = await context.TimetableVersions.AsNoTracking()
            .Select(v => new TimetableVersionSummary(v.Id, v.Status, v.EffectiveFrom)).ToListAsync(cancellationToken);
        var chosen = VersionResolver.Resolve(summaries, date);
        return chosen is null ? null : await GetReadOnlyAsync(chosen.Id, cancellationToken);
    }

    public Task<bool> HasOpenDraftAsync(CancellationToken cancellationToken) =>
        context.TimetableVersions.AnyAsync(v => v.Status == TimetableVersionStatus.Draft || v.Status == TimetableVersionStatus.InReview, cancellationToken);

    public Task<bool> EffectiveDateTakenAsync(DateOnly date, CancellationToken cancellationToken) =>
        context.TimetableVersions.AnyAsync(v => v.EffectiveFrom == date
            && (v.Status == TimetableVersionStatus.Published || v.Status == TimetableVersionStatus.Superseded), cancellationToken);

    public async Task<IReadOnlyList<TimetableVersion>> GetPublishedBeforeAsync(DateOnly date, CancellationToken cancellationToken) =>
        await context.TimetableVersions.Where(v => v.Status == TimetableVersionStatus.Published && v.EffectiveFrom < date).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Route>> GetRoutesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var wanted = ids.Distinct().ToList();
        return await context.Routes.AsNoTracking().Include(r => r.Stops).Where(r => wanted.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken);
    }

    public Task<Route?> GetRouteByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.Routes.AsNoTracking().Include(r => r.Stops).FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task<Guid?> GetCalendarIdAsync(string code, CancellationToken cancellationToken) =>
        await context.ServiceCalendars.AsNoTracking().Where(c => c.Code == code).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> GetCalendarCodesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var wanted = ids.Distinct().ToList();
        return await context.ServiceCalendars.AsNoTracking().Where(c => wanted.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Code, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, (string Code, string Name)>> GetStationsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var wanted = ids.Distinct().ToList();
        var rows = await context.Stations.AsNoTracking().Where(s => wanted.Contains(s.Id)).Select(s => new { s.Id, s.Code, s.Name }).ToListAsync(cancellationToken);
        return rows.ToDictionary(s => s.Id, s => (s.Code, s.Name));
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetStationIdsAsync(IEnumerable<string> codes, CancellationToken cancellationToken)
    {
        var wanted = codes.Distinct().ToList();
        return await context.Stations.AsNoTracking().Where(s => wanted.Contains(s.Code)).ToDictionaryAsync(s => s.Code, s => s.Id, cancellationToken);
    }

    public void Add(TimetableVersion version) => context.TimetableVersions.Add(version);

    // Guid keys are set by the code, so a new service must be added explicitly or EF would treat it as an existing row.
    public void AddService(Service service) => context.Services.Add(service);

    public void RemoveService(Service service) => context.Services.Remove(service);
}
