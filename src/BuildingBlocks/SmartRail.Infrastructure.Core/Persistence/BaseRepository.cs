using Microsoft.EntityFrameworkCore;
using SmartRail.SharedKernel.Abstractions;
using SmartRail.SharedKernel.Primitives;

namespace SmartRail.Infrastructure.Core.Persistence;

// Repositories never call SaveChanges; the module's unit of work does.
public abstract class BaseRepository<TEntity, TKey, TContext>(TContext context) : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
    where TContext : DbContext
{
    protected TContext Context { get; } = context;

    protected DbSet<TEntity> DbSet => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public void Add(TEntity entity) => DbSet.Add(entity);

    public void Remove(TEntity entity) => DbSet.Remove(entity);
}
