using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Modules.Network.Domain.Stations;

namespace SmartRail.Modules.Network.Infrastructure.Persistence.Configurations;

public sealed class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("stations", t => t.HasCheckConstraint("ck_stations_code", "code ~ '^[A-Z]{3,5}$'"));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(5);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.IsActive).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.Name);

        // Facility codes are kept as a text array on the station.
        builder.Ignore(s => s.Facilities);
        builder.Property<string[]>("_facilities").HasColumnName("facilities").IsRequired();

        builder.HasMany(s => s.Platforms).WithOne().HasForeignKey(p => p.StationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Platforms).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.Translations).WithOne().HasForeignKey(t => t.StationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PlatformConfiguration : IEntityTypeConfiguration<Platform>
{
    public void Configure(EntityTypeBuilder<Platform> builder)
    {
        builder.ToTable("platforms");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).IsRequired().HasMaxLength(3);
        builder.Property(p => p.IsAvailable).IsRequired();
        builder.HasIndex(p => new { p.StationId, p.Code }).IsUnique();

        // Target of the route stop's composite key: a stop's platform must belong to the stop's station.
        builder.HasAlternateKey(p => new { p.Id, p.StationId });
    }
}

public sealed class StationTranslationConfiguration : IEntityTypeConfiguration<StationTranslation>
{
    public void Configure(EntityTypeBuilder<StationTranslation> builder)
    {
        builder.ToTable("station_translations");
        builder.HasKey(t => new { t.StationId, t.LanguageCode });
        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.HasOne<SmartRail.Modules.Network.Domain.Languages.Language>()
            .WithMany()
            .HasForeignKey(t => t.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
