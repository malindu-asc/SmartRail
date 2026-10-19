using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
            .Where(d => d.Status != DisruptionStatus.Resolved && d.Status != DisruptionStatus.Retracted)
            .OrderByDescending(d => d.UpdatedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Disruption>> ListForConsoleAsync(DateTimeOffset closedSince, CancellationToken cancellationToken) =>
        await context.Disruptions.AsNoTracking()
            .Where(d => (d.Status != DisruptionStatus.Resolved && d.Status != DisruptionStatus.Retracted) || d.UpdatedAtUtc >= closedSince)
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
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Type).HasMaxLength(60);
        builder.Property(d => d.CreatedBy).IsRequired().HasMaxLength(64);
        builder.Property(d => d.ApprovedBy).HasMaxLength(64);
        builder.Property(d => d.RejectedBy).HasMaxLength(64);
        builder.Property(d => d.RejectionComment).HasMaxLength(500);
        builder.Property(d => d.StationCodes).AsJsonList();
        builder.HasIndex(d => d.Status).HasDatabaseName("ix_disruptions_status");
    }
}
