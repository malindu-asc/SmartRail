using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Stations;

namespace SmartRail.Modules.Network.Infrastructure.Persistence.Configurations;

public sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("routes");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).IsRequired().HasMaxLength(10);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.IsActive).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();

        builder.HasMany(r => r.Stops).WithOne().HasForeignKey(s => s.RouteId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Stops).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("route_stops", t =>
        {
            t.HasCheckConstraint("ck_route_stops_sequence", "sequence >= 1");
            t.HasCheckConstraint("ck_route_stops_minutes", "minutes_from_previous >= 0");
            t.HasCheckConstraint("ck_route_stops_dwell", "dwell_minutes >= 0");
            t.HasCheckConstraint("ck_route_stops_kind", "kind in ('Stop', 'PickUpOnly', 'SetDownOnly', 'Passing')");
        });

        builder.HasKey(s => new { s.RouteId, s.Sequence });
        builder.Property(s => s.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(s => new { s.RouteId, s.StationId }).IsUnique();

        builder.HasOne<Station>().WithMany().HasForeignKey(s => s.StationId).OnDelete(DeleteBehavior.Restrict);

        // The platform must belong to this stop's own station. When no default platform is set the key is not checked.
        builder.HasOne<Platform>()
            .WithMany()
            .HasForeignKey(s => new { s.DefaultPlatformId, s.StationId })
            .HasPrincipalKey(p => new { p.Id, p.StationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
