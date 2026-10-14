using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

public sealed class JourneysDbContext(DbContextOptions<JourneysDbContext> options)
    : BaseDbContext(options), IJourneysUnitOfWork
{
    public const string Schema = "journeys";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JourneysDbContext).Assembly);
    }
}
