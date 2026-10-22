using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartRail.Modules.Journeys.Domain.Crew;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence.Configurations;

public sealed class PlanApprovalConfiguration : IEntityTypeConfiguration<PlanApproval>
{
    public void Configure(EntityTypeBuilder<PlanApproval> builder)
    {
        builder.ToTable("plan_approvals");
        builder.HasKey(a => a.ServiceDate);
        builder.Property(a => a.ServiceDate).ValueGeneratedNever();
        builder.Property(a => a.ApprovedBy).HasMaxLength(100).IsRequired();
    }
}
