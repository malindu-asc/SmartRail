using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

// Lets `dotnet ef` run against this project without starting the host.
public sealed class JourneysDbContextFactory : IDesignTimeDbContextFactory<JourneysDbContext>
{
    public JourneysDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SMARTRAIL_DB")
            ?? "Host=localhost;Port=5432;Database=smartraildb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<JourneysDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsHistoryTable("__EFMigrationsHistory", JourneysDbContext.Schema))
            .Options;

        return new JourneysDbContext(options);
    }
}
