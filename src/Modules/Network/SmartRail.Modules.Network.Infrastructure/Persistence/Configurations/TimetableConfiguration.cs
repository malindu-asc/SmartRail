using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Stations;
using SmartRail.Modules.Network.Domain.Timetables;

namespace SmartRail.Modules.Network.Infrastructure.Persistence.Configurations;

public sealed class TimetableVersionConfiguration : IEntityTypeConfiguration<TimetableVersion>
{
    public void Configure(EntityTypeBuilder<TimetableVersion> builder)
    {
        builder.ToTable("timetable_versions", t =>
        {
            t.HasCheckConstraint("ck_timetable_versions_status", "status in ('Draft', 'InReview', 'Published', 'Superseded')");
            t.HasCheckConstraint("ck_timetable_versions_effective", "status in ('Draft', 'InReview') or effective_from is not null");
            t.HasCheckConstraint("ck_timetable_versions_approver", "status not in ('Published', 'Superseded') or (approved_by is not null and approved_by <> author)");
        });

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Label).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(v => v.Author).IsRequired().HasMaxLength(64);
        builder.Property(v => v.ApprovedBy).HasMaxLength(64);

        // At most one version takes effect on a given date.
        builder.HasIndex(v => v.EffectiveFrom)
            .IsUnique()
            .HasDatabaseName("ux_timetable_effective")
            .HasFilter("status in ('Published', 'Superseded')");

        builder.HasMany(v => v.Services).WithOne().HasForeignKey(s => s.TimetableVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(v => v.Services).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ServiceCalendarConfiguration : IEntityTypeConfiguration<ServiceCalendar>
{
    public void Configure(EntityTypeBuilder<ServiceCalendar> builder)
    {
        builder.ToTable("service_calendars", t =>
            t.HasCheckConstraint("ck_service_calendars_range", "valid_to is null or valid_to >= valid_from"));

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.Code).IsUnique();

        builder.HasMany(c => c.Exceptions).WithOne().HasForeignKey(e => e.CalendarId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Exceptions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class CalendarExceptionConfiguration : IEntityTypeConfiguration<CalendarException>
{
    public void Configure(EntityTypeBuilder<CalendarException> builder)
    {
        builder.ToTable("calendar_exceptions", t =>
            t.HasCheckConstraint("ck_calendar_exceptions_kind", "kind in ('Removed', 'Added')"));

        builder.HasKey(e => new { e.CalendarId, e.Date });
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(200);
    }
}

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("services", t =>
        {
            t.HasCheckConstraint("ck_services_direction", "direction in ('Forward', 'Reverse')");
            t.HasCheckConstraint("ck_services_category", "service_category in ('Regional', 'Intercity', 'HighSpeed')");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.TrainNumber).IsRequired().HasMaxLength(10);
        builder.Property(s => s.Direction).HasConversion<string>().HasMaxLength(7).IsRequired();
        builder.Property(s => s.Category).HasColumnName("service_category").HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.HasIndex(s => new { s.TimetableVersionId, s.TrainNumber }).IsUnique();
        builder.HasIndex(s => s.RouteId);

        builder.HasOne<Route>().WithMany().HasForeignKey(s => s.RouteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ServiceCalendar>().WithMany().HasForeignKey(s => s.CalendarId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Stops).WithOne().HasForeignKey(st => st.ServiceId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Stops).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ServiceStopConfiguration : IEntityTypeConfiguration<ServiceStop>
{
    public void Configure(EntityTypeBuilder<ServiceStop> builder)
    {
        builder.ToTable("service_stops", t =>
        {
            t.HasCheckConstraint("ck_service_stops_sequence", "sequence >= 1");
            t.HasCheckConstraint("ck_service_stops_arrival", "arrival_minutes between 0 and 2879");
            t.HasCheckConstraint("ck_service_stops_departure", "departure_minutes between 0 and 2879");
            t.HasCheckConstraint("ck_service_stops_has_time", "arrival_minutes is not null or departure_minutes is not null");
            t.HasCheckConstraint("ck_service_stops_order", "arrival_minutes is null or departure_minutes is null or arrival_minutes <= departure_minutes");
            t.HasCheckConstraint("ck_service_stops_kind", "kind in ('Stop', 'PickUpOnly', 'SetDownOnly', 'Passing')");
        });

        builder.HasKey(s => new { s.ServiceId, s.Sequence });
        builder.Property(s => s.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(s => new { s.ServiceId, s.StationId }).IsUnique();
        builder.HasIndex(s => new { s.StationId, s.DepartureMinutes });

        builder.HasOne<Station>().WithMany().HasForeignKey(s => s.StationId).OnDelete(DeleteBehavior.Restrict);

        // The platform must belong to this stop's own station; no default platform means the key is not checked.
        builder.HasOne<Platform>()
            .WithMany()
            .HasForeignKey(s => new { s.DefaultPlatformId, s.StationId })
            .HasPrincipalKey(p => new { p.Id, p.StationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
