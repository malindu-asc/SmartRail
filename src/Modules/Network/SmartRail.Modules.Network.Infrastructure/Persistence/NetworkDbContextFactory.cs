using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartRail.Modules.Network.Infrastructure.Persistence;

// Lets `dotnet ef` run against this project without starting the host.
public sealed class NetworkDbContextFactory : IDesignTimeDbContextFactory<NetworkDbContext>
{
    public NetworkDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SMARTRAIL_DB")
            ?? "Host=localhost;Port=5432;Database=smartraildb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<NetworkDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsHistoryTable("__EFMigrationsHistory", NetworkDbContext.Schema))
            .Options;

        return new NetworkDbContext(options);
    }
}
