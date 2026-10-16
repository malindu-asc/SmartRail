using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Modules.Journeys.Domain.Crew;
using SmartRail.Modules.Journeys.Domain.Fleet;
using SmartRail.Modules.Journeys.Domain.Journeys;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence.Configurations;

public sealed class JourneyConfiguration : IEntityTypeConfiguration<Journey>
{
    public void Configure(EntityTypeBuilder<Journey> builder)
    {
        builder.ToTable("journeys", t =>
        {
            t.HasCheckConstraint("ck_journeys_lifecycle", "lifecycle in ('Planned','Ready','Running','Interrupted','Completed','TerminatedShort','Cancelled','TerminatedEmergency')");
            t.HasCheckConstraint("ck_journeys_sub_state", "running_sub_state is null or lifecycle = 'Running'");
            t.HasCheckConstraint("ck_journeys_interrupted_delay", "lifecycle <> 'Interrupted' or delay_minutes is null");
            t.HasCheckConstraint("ck_journeys_manual_reason", "creation_origin <> 'Manual' or manual_reason is not null");
            t.HasCheckConstraint("ck_journeys_times", "scheduled_arrival_utc > scheduled_departure_utc");
        });

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();
        builder.Property(j => j.TrainNumber).IsRequired().HasMaxLength(10);
        builder.Property(j => j.RouteCode).IsRequired().HasMaxLength(10);
        builder.Property(j => j.OriginStationCode).IsRequired().HasMaxLength(5);
        builder.Property(j => j.DestinationStationCode).IsRequired().HasMaxLength(5);
        builder.Property(j => j.CreationOrigin).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(j => j.ManualReason).HasMaxLength(300);
        builder.Property(j => j.Lifecycle).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(j => j.RunningSubState).HasConversion<string>().HasMaxLength(10);
        builder.Property(j => j.PunctualityBand).HasConversion<string>().HasMaxLength(12);
        builder.Property(j => j.Confidence).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(j => j.CancelReasonCode).HasMaxLength(30);
        builder.Property(j => j.Version).IsConcurrencyToken();

        // Generation is idempotent: one journey per service and service date (REQ-LIVE-008).
        builder.HasIndex(j => new { j.ServiceId, j.ServiceDate })
            .IsUnique()
            .HasDatabaseName("ux_journeys_service_date")
            .HasFilter("service_id is not null");
        builder.HasIndex(j => new { j.ServiceDate, j.Lifecycle }).HasDatabaseName("ix_journeys_date_lifecycle");
        builder.HasIndex(j => j.ScheduledDepartureUtc)
            .HasDatabaseName("ix_journeys_active")
            .HasFilter("lifecycle in ('Planned', 'Ready', 'Running', 'Interrupted')");

        builder.HasMany(j => j.Stops).WithOne().HasForeignKey(s => s.JourneyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(j => j.Stops).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(j => j.VehicleAssignments).WithOne().HasForeignKey(a => a.JourneyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(j => j.VehicleAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(j => j.DriverAssignments).WithOne().HasForeignKey(a => a.JourneyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(j => j.DriverAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class JourneyStopConfiguration : IEntityTypeConfiguration<JourneyStop>
{
    public void Configure(EntityTypeBuilder<JourneyStop> builder)
    {
        builder.ToTable("journey_stops", t =>
        {
            t.HasCheckConstraint("ck_journey_stops_sequence", "sequence >= 1");
            t.HasCheckConstraint("ck_journey_stops_kind", "kind in ('Stop','PickUpOnly','SetDownOnly','Passing')");
            t.HasCheckConstraint("ck_journey_stops_state", "state in ('Pending','Arrived','Departed','Skipped','Cancelled')");
            t.HasCheckConstraint("ck_journey_stops_source", "estimate_source in ('Confirmed','Live','Scheduled')");
            t.HasCheckConstraint("ck_journey_stops_platform_state", "platform_state in ('TBA','Assigned','Changed')");
            t.HasCheckConstraint("ck_journey_stops_dwell", "dwell_minutes >= 0");
        });

        builder.HasKey(s => new { s.JourneyId, s.Sequence });
        builder.Property(s => s.StationCode).IsRequired().HasMaxLength(5);
        builder.Property(s => s.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.State).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(s => s.EstimateSource).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(s => s.PlatformCode).HasMaxLength(3);
        builder.Property(s => s.PlatformState).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(s => s.PlatformOriginalCode).HasMaxLength(3);
        builder.Property(s => s.PlatformChangedBy).HasMaxLength(64);

        // The departure board reads by station and scheduled time (REQ-NFR-002).
        builder.HasIndex(s => new { s.StationCode, s.ScheduledDepartureUtc })
            .HasDatabaseName("ix_journey_stops_board")
            .HasFilter("kind <> 'Passing'");
        builder.HasIndex(s => new { s.StationCode, s.ScheduledArrivalUtc })
            .HasDatabaseName("ix_journey_stops_arrivals")
            .HasFilter("kind <> 'Passing'");
    }
}

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles", t => t.HasCheckConstraint("ck_vehicles_kind", "kind in ('Trainset','Locomotive')"));
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Code).IsRequired().HasMaxLength(30);
        builder.Property(v => v.Name).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Kind).HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.HasIndex(v => v.Code).IsUnique();
    }
}

public sealed class VehicleAssignmentConfiguration : IEntityTypeConfiguration<VehicleAssignment>
{
    public void Configure(EntityTypeBuilder<VehicleAssignment> builder)
    {
        builder.ToTable("vehicle_assignments", t => t.HasCheckConstraint("ck_vehicle_assignments_period", "valid_to is null or valid_to > valid_from"));
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.AssignedBy).IsRequired().HasMaxLength(64);
        builder.Property(a => a.Reason).HasMaxLength(300);
        builder.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at").HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(a => a.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.JourneyId);
        // The exclusion constraints (one vehicle per journey at a time, no vehicle on two journeys at once) are in the migration.
    }
}

public sealed class CrewDutyConfiguration : IEntityTypeConfiguration<CrewDuty>
{
    public void Configure(EntityTypeBuilder<CrewDuty> builder)
    {
        builder.ToTable("crew_duties", t =>
        {
            t.HasCheckConstraint("ck_crew_duties_window", "rostered_to > rostered_from");
            t.HasCheckConstraint("ck_crew_duties_state", "state in ('Rostered','SignedOn','OnJourney','SignedOff','Orphaned')");
            t.HasCheckConstraint("ck_crew_duties_review", "review_state in ('None','NeedsReview','Reviewed')");
        });

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.DriverSubject).IsRequired().HasMaxLength(64);
        builder.Property(d => d.State).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(d => d.ReviewState).HasConversion<string>().HasMaxLength(11).IsRequired();
        builder.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at").HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.HasOne<CrewDuty>().WithMany().HasForeignKey(d => d.HandoverFromDutyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => new { d.DriverSubject, d.ServiceDate });
        // A driver's rostered windows cannot overlap: exclusion constraint in the migration.
    }
}

public sealed class DriverAssignmentConfiguration : IEntityTypeConfiguration<DriverAssignment>
{
    public void Configure(EntityTypeBuilder<DriverAssignment> builder)
    {
        builder.ToTable("driver_assignments", t => t.HasCheckConstraint("ck_driver_assignments_period", "valid_to is null or valid_to > valid_from"));
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.AssignedBy).IsRequired().HasMaxLength(64);
        builder.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at").HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.HasOne<CrewDuty>().WithMany().HasForeignKey(a => a.DutyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.JourneyId);
        builder.HasIndex(a => a.DutyId);
        // One duty on a journey at a time (so a handover closes the outgoing duty first): exclusion constraint in the migration.
    }
}
