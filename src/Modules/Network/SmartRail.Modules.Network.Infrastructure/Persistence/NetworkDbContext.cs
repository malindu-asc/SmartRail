using Microsoft.EntityFrameworkCore;
using SmartRail.Infrastructure.Core.Audit;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Domain.Languages;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Stations;
using SmartRail.Modules.Network.Domain.Timetables;

namespace SmartRail.Modules.Network.Infrastructure.Persistence;

public sealed class NetworkDbContext(DbContextOptions<NetworkDbContext> options)
    : BaseDbContext(options), INetworkUnitOfWork
{
    public const string Schema = "network";

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<TimetableVersion> TimetableVersions => Set<TimetableVersion>();
    public DbSet<ServiceCalendar> ServiceCalendars => Set<ServiceCalendar>();
    public DbSet<Service> Services => Set<Service>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NetworkDbContext).Assembly);

        modelBuilder.AddAuditEntries();   // this module's audit_entries table (REQ-OPS-011)

        // Network tables use snake_case column names (the outbox table owned by the base context is left alone).
        modelBuilder.UseSnakeCaseColumns("SmartRail.Modules.Network");
    }
}
