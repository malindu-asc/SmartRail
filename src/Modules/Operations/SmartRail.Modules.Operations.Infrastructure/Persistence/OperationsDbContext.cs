using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Infrastructure.Core.Audit;
using SmartRail.Modules.Operations.Domain.Disruptions;
using SmartRail.Modules.Operations.Domain.Incidents;
using SmartRail.Modules.Operations.Domain.Messages;

namespace SmartRail.Modules.Operations.Infrastructure.Persistence;

public sealed class OperationsDbContext(DbContextOptions<OperationsDbContext> options)
    : BaseDbContext(options), IOperationsUnitOfWork
{
    public const string Schema = "operations";

    public DbSet<Disruption> Disruptions => Set<Disruption>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentEntry> IncidentEntries => Set<IncidentEntry>();
    public DbSet<ControlMessage> ControlMessages => Set<ControlMessage>();
    public DbSet<AssistanceRequest> AssistanceRequests => Set<AssistanceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationsDbContext).Assembly);
        modelBuilder.AddAuditEntries();   // this module's audit_entries table (REQ-OPS-011)
        modelBuilder.UseSnakeCaseColumns("SmartRail.Modules.Operations");
    }
}
