using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Crew;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

internal sealed class PlanApprovals(JourneysDbContext context, TimeProvider clock) : IPlanApprovals
{
    public async Task<IReadOnlySet<DateOnly>> ApprovedAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        (await context.PlanApprovals.AsNoTracking().Where(a => a.ServiceDate >= from && a.ServiceDate <= to).Select(a => a.ServiceDate).ToListAsync(cancellationToken)).ToHashSet();

    public async Task ApproveAsync(IReadOnlyList<DateOnly> dates, string approvedBy, CancellationToken cancellationToken)
    {
        var existing = await context.PlanApprovals.Where(a => dates.Contains(a.ServiceDate)).Select(a => a.ServiceDate).ToListAsync(cancellationToken);
        foreach (var date in dates.Except(existing))
            context.PlanApprovals.Add(new PlanApproval(date, approvedBy, clock.GetUtcNow()));

        await context.SaveChangesAsync(cancellationToken);
    }
}
