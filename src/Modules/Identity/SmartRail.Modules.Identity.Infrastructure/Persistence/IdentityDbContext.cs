using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : BaseDbContext(options), IIdentityUnitOfWork
{
    public const string Schema = "identity";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
