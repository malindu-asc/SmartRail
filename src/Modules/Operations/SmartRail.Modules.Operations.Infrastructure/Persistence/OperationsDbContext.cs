using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Operations.Infrastructure.Persistence;

public sealed class OperationsDbContext(DbContextOptions<OperationsDbContext> options)
    : BaseDbContext(options), IOperationsUnitOfWork
{
    public const string Schema = "operations";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationsDbContext).Assembly);
    }
}
