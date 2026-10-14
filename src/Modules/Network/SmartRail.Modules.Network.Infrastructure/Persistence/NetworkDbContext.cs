using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Network.Infrastructure.Persistence;

public sealed class NetworkDbContext(DbContextOptions<NetworkDbContext> options)
    : BaseDbContext(options), INetworkUnitOfWork
{
    public const string Schema = "network";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NetworkDbContext).Assembly);
    }
}
