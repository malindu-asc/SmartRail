using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Modules.Journeys.Domain.Live;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence.Configurations;

public sealed class JourneyEventConfiguration : IEntityTypeConfiguration<JourneyEvent>
{
    public void Configure(EntityTypeBuilder<JourneyEvent> builder)
    {
        builder.ToTable("journey_events", t =>
        {
            t.HasCheckConstraint("ck_journey_events_type", "event_type in ('Arrived','Departed','Passed')");
            t.HasCheckConstraint("ck_journey_events_source_kind", "source_kind in ('Feed','Driver','Controller','System')");
            t.HasCheckConstraint("ck_journey_events_disposition", "disposition in ('Applied','Superseded','Rejected','HeldForCorrection','Duplicate','Orphaned','OutsideDutyWindow')");
            t.HasCheckConstraint("ck_journey_events_rank", "source_rank >= 1");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.SourceKind).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(e => e.Disposition).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.DispositionReason).HasMaxLength(300);
        builder.Property(e => e.Payload).HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");

        builder.HasOne<SmartRail.Modules.Journeys.Domain.Journeys.Journey>().WithMany().HasForeignKey(e => e.JourneyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.SupersededBy).WithMany().HasForeignKey(e => e.SupersededByEventId).OnDelete(DeleteBehavior.Restrict);

        // A message is received once per source: a replay is a harmless no-op (REQ-LIVE-005).
        builder.HasIndex(e => new { e.SourceId, e.DeviceSequence }).IsUnique().HasDatabaseName("ux_events_source_sequence");
        builder.HasIndex(e => new { e.JourneyId, e.ReceivedAt }).HasDatabaseName("ix_events_journey");
        builder.HasIndex(e => new { e.JourneyId, e.StopSequence, e.EventType })
            .HasDatabaseName("ix_events_fact")
            .HasFilter("disposition = 'Applied'");
    }
}

public sealed class TelemetryReadingConfiguration : IEntityTypeConfiguration<TelemetryReading>
{
    public void Configure(EntityTypeBuilder<TelemetryReading> builder)
    {
        builder.ToTable("telemetry_readings", t =>
        {
            t.HasCheckConstraint("ck_telemetry_progress", "progress is null or (progress >= 0 and progress <= 1)");
            t.HasCheckConstraint("ck_telemetry_speed", "speed_kmh is null or speed_kmh >= 0");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.VehicleCode).IsRequired().HasMaxLength(30);
        builder.Property(r => r.SectionFromStationCode).HasMaxLength(5);
        builder.Property(r => r.SectionToStationCode).HasMaxLength(5);
        builder.Property(r => r.Progress).HasPrecision(5, 4);
        builder.Property(r => r.SpeedKmh).HasPrecision(5, 1);
        builder.Property(r => r.RejectReason).HasMaxLength(100);

        builder.HasIndex(r => new { r.SourceId, r.Sequence }).IsUnique().HasDatabaseName("ux_telemetry_source_sequence");
        builder.HasIndex(r => new { r.VehicleCode, r.RecordedAt }).IsDescending(false, true).HasDatabaseName("ix_telemetry_vehicle");
        builder.HasIndex(r => r.ReceivedAt).HasDatabaseName("ix_telemetry_purge").HasFilter("legal_hold = false");
    }
}
