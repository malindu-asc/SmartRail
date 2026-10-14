using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartRail.SharedKernel.Primitives;

namespace SmartRail.Infrastructure.Core.Persistence;

public sealed class AuditableEntitiesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var now = timeProvider.GetUtcNow();
            foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAtUtc = now;
                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAtUtc = now;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
