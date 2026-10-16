using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Modules.Network.Domain.Languages;

namespace SmartRail.Modules.Network.Infrastructure.Persistence.Configurations;

public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("languages");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("code").HasMaxLength(10);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
        builder.Property(l => l.IsDefault).IsRequired();

        // At most one default language.
        builder.HasIndex(l => l.IsDefault).IsUnique().HasFilter("is_default");
    }
}
