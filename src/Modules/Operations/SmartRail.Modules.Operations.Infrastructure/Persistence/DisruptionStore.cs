using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Modules.Operations.Application.Disruptions;
using SmartRail.Modules.Operations.Domain.Disruptions;

namespace SmartRail.Modules.Operations.Infrastructure.Persistence;

internal sealed class DisruptionStore(OperationsDbContext context) : IDisruptionStore
{
    public Task<Disruption?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Disruptions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Disruption>> ListPublishedAsync(CancellationToken cancellationToken) =>
        await context.Disruptions.AsNoTracking().Where(d => d.Status == DisruptionStatus.Published)
            .OrderByDescending(d => d.UpdatedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Disruption>> ListOpenAsync(CancellationToken cancellationToken) =>
        await context.Disruptions.AsNoTracking()
            .Where(d => d.Status == DisruptionStatus.Draft || d.Status == DisruptionStatus.Published)
            .OrderByDescending(d => d.UpdatedAtUtc).ToListAsync(cancellationToken);

    public void Add(Disruption disruption) => context.Disruptions.Add(disruption);
}

public sealed class DisruptionConfiguration : IEntityTypeConfiguration<Disruption>
{
    public void Configure(EntityTypeBuilder<Disruption> builder)
    {
        builder.ToTable("disruptions");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Title).IsRequired().HasMaxLength(120);
        builder.Property(d => d.Message).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.Severity).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(d => d.CreatedBy).IsRequired().HasMaxLength(64);
        builder.Property(d => d.ApprovedBy).HasMaxLength(64);
        builder.Property(d => d.StationCodes).HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s)),
                v => v.ToList()));
        builder.HasIndex(d => d.Status).HasDatabaseName("ix_disruptions_status");
    }
}
