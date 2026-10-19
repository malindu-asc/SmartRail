using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Modules.Identity.Domain.Feeds;

namespace SmartRail.Modules.Identity.Infrastructure.Feeds;

public sealed class FeedSourceConfiguration : IEntityTypeConfiguration<FeedSource>
{
    public void Configure(EntityTypeBuilder<FeedSource> builder)
    {
        builder.ToTable("feed_sources", t => t.HasCheckConstraint("ck_feed_sources_rank", "precedence_rank >= 1"));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.Code).HasColumnName("code").IsRequired().HasMaxLength(30);
        builder.Property(s => s.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(s => s.KeyHash).HasColumnName("key_hash").IsRequired().HasMaxLength(128);
        builder.Property(s => s.PrecedenceRank).HasColumnName("precedence_rank");
        builder.Property(s => s.IsActive).HasColumnName("is_active");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.RotatedAt).HasColumnName("rotated_at");
        builder.Property(s => s.LastUsedAt).HasColumnName("last_used_at");
        builder.HasIndex(s => s.Code).IsUnique();
    }
}
