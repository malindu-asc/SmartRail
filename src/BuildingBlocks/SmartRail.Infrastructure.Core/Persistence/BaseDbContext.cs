using Microsoft.EntityFrameworkCore;
using SmartRail.Infrastructure.Core.Outbox;
using SmartRail.SharedKernel.Abstractions;

namespace SmartRail.Infrastructure.Core.Persistence;

// Every module DbContext inherits from this. OutboxMessages maps into the module's own schema.
public abstract class BaseDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
