using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartRail.Modules.Notifications.Infrastructure.Persistence;

// Lets `dotnet ef` run against this project without starting the host.
public sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SMARTRAIL_DB")
            ?? "Host=localhost;Port=5432;Database=smartraildb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsHistoryTable("__EFMigrationsHistory", NotificationsDbContext.Schema))
            .Options;

        return new NotificationsDbContext(options);
    }
}
