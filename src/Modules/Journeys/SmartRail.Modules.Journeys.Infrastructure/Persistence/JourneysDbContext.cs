using Microsoft.EntityFrameworkCore;
using SmartRail.Infrastructure.Core.Audit;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Fleet;
using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

public sealed class JourneysDbContext(DbContextOptions<JourneysDbContext> options)
    : BaseDbContext(options), IJourneysUnitOfWork
{
    public const string Schema = "journeys";

    public DbSet<Journey> Journeys => Set<Journey>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<CrewDuty> CrewDuties => Set<CrewDuty>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JourneysDbContext).Assembly);
        modelBuilder.AddAuditEntries();   // this module's audit_entries table (REQ-OPS-011)
        modelBuilder.UseSnakeCaseColumns("SmartRail.Modules.Journeys");
    }
}
