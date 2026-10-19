using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Infrastructure.Core.Audit;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Modules.Operations.Domain.Incidents;
using SmartRail.Modules.Operations.Domain.Messages;

namespace SmartRail.Modules.Operations.Infrastructure.Persistence;

internal sealed class OperationsAuditLog(OperationsDbContext context, TimeProvider clock)
    : AuditLogWriter(context, clock), IOperationsAuditLog;

internal sealed class IncidentStore(OperationsDbContext context) : IIncidentStore
{
    public Task<Incident?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Incidents.Include(i => i.Entries).FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Incident>> ListAsync(bool openOnly, CancellationToken cancellationToken) =>
        await context.Incidents.AsNoTracking().Include(i => i.Entries)
            .Where(i => !openOnly || i.Status == IncidentStatus.Open)
            .OrderByDescending(i => i.OpenedAtUtc).Take(200).ToListAsync(cancellationToken);

    public Task<int> CountOpenOwnedByAsync(string ownerSubject, CancellationToken cancellationToken) =>
        context.Incidents.CountAsync(i => i.Status == IncidentStatus.Open && i.OwnerSubject == ownerSubject, cancellationToken);

    public void Add(Incident incident) => context.Incidents.Add(incident);
}

internal sealed class MessageStore(OperationsDbContext context) : IMessageStore
{
    public Task<ControlMessage?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.ControlMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ControlMessage>> ListSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        await context.ControlMessages.AsNoTracking().Where(m => m.SentAtUtc >= since)
            .OrderByDescending(m => m.SentAtUtc).Take(200).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ControlMessage>> ListForDriverAsync(string driverSubject, DateTimeOffset since, CancellationToken cancellationToken) =>
        await context.ControlMessages.Where(m => m.DriverSubject == driverSubject && m.SentAtUtc >= since)
            .OrderByDescending(m => m.SentAtUtc).Take(50).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ControlMessage>> ListForIncidentAsync(Guid incidentId, CancellationToken cancellationToken) =>
        await context.ControlMessages.AsNoTracking().Where(m => m.IncidentId == incidentId)
            .OrderBy(m => m.SentAtUtc).ToListAsync(cancellationToken);

    public void Add(ControlMessage message) => context.ControlMessages.Add(message);
}

internal sealed class AssistanceStore(OperationsDbContext context) : IAssistanceStore
{
    public Task<AssistanceRequest?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.AssistanceRequests.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AssistanceRequest>> ListOpenAsync(CancellationToken cancellationToken) =>
        await context.AssistanceRequests.AsNoTracking().Where(a => a.Status == AssistanceStatus.Open)
            .OrderBy(a => a.RequestedAtUtc).ToListAsync(cancellationToken);

    public void Add(AssistanceRequest request) => context.AssistanceRequests.Add(request);
}

internal static class JsonList
{
    public static PropertyBuilder<List<string>> AsJsonList(this PropertyBuilder<List<string>> property)
    {
        property.HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
        property.Metadata.SetValueComparer(new ValueComparer<List<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s)),
            v => v.ToList()));
        return property;
    }
}

public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.Title).IsRequired().HasMaxLength(120);
        builder.Property(i => i.Severity).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(i => i.OwnerSubject).IsRequired().HasMaxLength(64);
        builder.Property(i => i.OpenedBy).IsRequired().HasMaxLength(64);
        builder.Property(i => i.ReportedBy).HasMaxLength(120);
        builder.Property(i => i.ResolvedBy).HasMaxLength(64);
        builder.Property(i => i.ResolutionNote).HasMaxLength(300);
        builder.Property(i => i.StationCodes).AsJsonList();
        builder.HasMany(i => i.Entries).WithOne().HasForeignKey(e => e.IncidentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(i => i.Status).HasDatabaseName("ix_incidents_status");
    }
}

public sealed class IncidentEntryConfiguration : IEntityTypeConfiguration<IncidentEntry>
{
    public void Configure(EntityTypeBuilder<IncidentEntry> builder)
    {
        builder.ToTable("incident_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(e => e.Text).IsRequired().HasMaxLength(300);
        builder.Property(e => e.Detail).HasMaxLength(300);
        builder.Property(e => e.Author).IsRequired().HasMaxLength(64);
        builder.HasIndex(e => new { e.IncidentId, e.At }).HasDatabaseName("ix_incident_entries_incident_at");
    }
}

public sealed class ControlMessageConfiguration : IEntityTypeConfiguration<ControlMessage>
{
    public void Configure(EntityTypeBuilder<ControlMessage> builder)
    {
        builder.ToTable("control_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.DriverSubject).IsRequired().HasMaxLength(64);
        builder.Property(m => m.TrainNumber).HasMaxLength(20);
        builder.Property(m => m.UnitCode).HasMaxLength(20);
        builder.Property(m => m.Template).HasMaxLength(60);
        builder.Property(m => m.Body).IsRequired().HasMaxLength(500);
        builder.Property(m => m.SentBy).IsRequired().HasMaxLength(64);
        builder.HasIndex(m => new { m.DriverSubject, m.SentAtUtc }).HasDatabaseName("ix_control_messages_driver_sent");
        builder.HasIndex(m => m.IncidentId).HasDatabaseName("ix_control_messages_incident");
    }
}

public sealed class AssistanceRequestConfiguration : IEntityTypeConfiguration<AssistanceRequest>
{
    public void Configure(EntityTypeBuilder<AssistanceRequest> builder)
    {
        builder.ToTable("assistance_requests");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.DriverSubject).IsRequired().HasMaxLength(64);
        builder.Property(a => a.TrainNumber).HasMaxLength(20);
        builder.Property(a => a.StationCode).HasMaxLength(10);
        builder.Property(a => a.Note).HasMaxLength(300);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(a => a.HandledBy).HasMaxLength(64);
        builder.Property(a => a.Outcome).HasMaxLength(300);
        builder.HasIndex(a => a.Status).HasDatabaseName("ix_assistance_requests_status");
    }
}
