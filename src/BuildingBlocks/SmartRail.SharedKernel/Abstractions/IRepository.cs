using SmartRail.SharedKernel.Primitives;

namespace SmartRail.SharedKernel.Abstractions;

public interface IRepository<TEntity, in TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void Remove(TEntity entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
