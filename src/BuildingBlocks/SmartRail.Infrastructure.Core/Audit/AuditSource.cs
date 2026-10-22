using Microsoft.EntityFrameworkCore;
using SmartRail.Application.Core.Abstractions;

namespace SmartRail.Infrastructure.Core.Audit;

// Reads the audit_entries table of one module context. Register one per module:
// services.AddScoped<IAuditSource>(sp => new AuditSource<JourneysDbContext>(sp.GetRequiredService<JourneysDbContext>(), "journeys", false));
public sealed class AuditSource<TContext>(TContext context, string module, bool isAccess) : IAuditSource
    where TContext : DbContext
{
    public string Module => module;

    public bool IsAccess => isAccess;

    public async Task<IReadOnlyList<AuditRow>> QueryAsync(AuditFilter filter, CancellationToken cancellationToken)
    {
        var query = context.Set<AuditEntry>().AsNoTracking().AsQueryable();

        if (filter.From is { } from)
            query = query.Where(e => e.OccurredAt >= from);
        if (filter.To is { } to)
            query = query.Where(e => e.OccurredAt < to);
        if (!string.IsNullOrWhiteSpace(filter.Actor))
            query = query.Where(e => e.ActorSubject == filter.Actor);
        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(e => e.Action == filter.Action);
        if (!string.IsNullOrWhiteSpace(filter.Text))
        {
            var text = filter.Text.Trim().ToLowerInvariant();
            query = query.Where(e => e.Action.ToLower().Contains(text)
                || e.TargetType.ToLower().Contains(text)
                || (e.TargetId != null && e.TargetId.ToLower().Contains(text))
                || (e.Reason != null && e.Reason.ToLower().Contains(text)));
        }

        var rows = await query.OrderByDescending(e => e.OccurredAt).Take(Math.Clamp(filter.Take, 1, 500)).ToListAsync(cancellationToken);
        return rows.Select(e => new AuditRow(
            e.Id, module, e.OccurredAt, e.ActorSubject, e.ActorRole, e.Action, e.TargetType, e.TargetId, e.Reason,
            e.BeforeState, e.AfterState)).ToList();
    }
}
