using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Modules.Operations.Domain.Disruptions;

namespace SmartRail.Modules.Operations.Infrastructure.Persistence;

public sealed class OperationsDbContext(DbContextOptions<OperationsDbContext> options)
    : BaseDbContext(options), IOperationsUnitOfWork
{
    public const string Schema = "operations";

    public DbSet<Disruption> Disruptions => Set<Disruption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationsDbContext).Assembly);
        modelBuilder.UseSnakeCaseColumns("SmartRail.Modules.Operations");
    }
}
