using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartRail.Modules.Operations.Infrastructure.Persistence;

// Lets `dotnet ef` run against this project without starting the host.
public sealed class OperationsDbContextFactory : IDesignTimeDbContextFactory<OperationsDbContext>
{
    public OperationsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SMARTRAIL_DB")
            ?? "Host=localhost;Port=5432;Database=smartraildb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<OperationsDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsHistoryTable("__EFMigrationsHistory", OperationsDbContext.Schema))
            .Options;

        return new OperationsDbContext(options);
    }
}
