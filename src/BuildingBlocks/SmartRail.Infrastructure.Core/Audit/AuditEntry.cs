using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Application.Core.Abstractions;

namespace SmartRail.Infrastructure.Core.Audit;

// One row of a module's audit_entries table. Append-only: a database trigger refuses update and delete.
public sealed class AuditEntry
{
    public long Id { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string ActorSubject { get; private set; } = string.Empty;
    public string? ActorRole { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string TargetType { get; private set; } = string.Empty;
    public string? TargetId { get; private set; }
    public string? Reason { get; private set; }
    public string? BeforeState { get; private set; }
    public string? AfterState { get; private set; }
    public string? CorrelationId { get; private set; }

    internal static AuditEntry From(AuditRecord record, DateTimeOffset now) => new()
    {
        OccurredAt = now,
        ActorSubject = record.ActorSubject,
        ActorRole = record.ActorRole,
        Action = record.Action,
        TargetType = record.TargetType,
        TargetId = record.TargetId,
        Reason = record.Reason,
        BeforeState = record.Before is null ? null : JsonSerializer.Serialize(record.Before),
        AfterState = record.After is null ? null : JsonSerializer.Serialize(record.After),
        CorrelationId = record.CorrelationId
    };
}

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(e => e.ActorSubject).HasColumnName("actor_subject").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ActorRole).HasColumnName("actor_role").HasMaxLength(30);
        builder.Property(e => e.Action).HasColumnName("action").HasMaxLength(80).IsRequired();
        builder.Property(e => e.TargetType).HasColumnName("target_type").HasMaxLength(40).IsRequired();
        builder.Property(e => e.TargetId).HasColumnName("target_id").HasMaxLength(64);
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(e => e.BeforeState).HasColumnName("before_state").HasColumnType("jsonb");
        builder.Property(e => e.AfterState).HasColumnName("after_state").HasColumnType("jsonb");
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
        builder.HasIndex(e => e.OccurredAt).IsDescending();
        builder.HasIndex(e => new { e.TargetType, e.TargetId });
    }
}

public static class AuditModelBuilderExtensions
{
    // Call from a module DbContext's OnModelCreating to give that module an audit_entries table.
    public static ModelBuilder AddAuditEntries(this ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new AuditEntryConfiguration());
}

// Adds audit rows to a module's context; the module's unit of work saves them with the change.
public class AuditLogWriter(DbContext context, TimeProvider clock) : IAuditLog
{
    public void Record(AuditRecord record) =>
        context.Set<AuditEntry>().Add(AuditEntry.From(record, clock.GetUtcNow()));
}
