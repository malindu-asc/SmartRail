using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartRail.Infrastructure.Core.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Type { get; init; }
    public required string Content { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public DateTimeOffset? DeadLetteredOnUtc { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).IsRequired().HasMaxLength(500);
        builder.Property(m => m.Content).IsRequired().HasColumnType("jsonb");
        builder.HasIndex(m => m.OccurredOnUtc).HasFilter("\"ProcessedOnUtc\" IS NULL AND \"DeadLetteredOnUtc\" IS NULL");
    }
}
