using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SmartRail.Api.IntegrationTests.Authentication;
using Testcontainers.PostgreSql;

namespace SmartRail.Api.IntegrationTests.Network;

// One Postgres container for the whole test run. Every test class gets its own database in it.
internal static class SharedPostgres
{
    private static readonly Lazy<PostgreSqlContainer> Container = new(() =>
    {
        var container = new PostgreSqlBuilder("postgres:16").Build();
        container.StartAsync().GetAwaiter().GetResult();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => container.DisposeAsync().AsTask().GetAwaiter().GetResult();
        return container;
    });

    internal static string CreateDatabase()
    {
        var container = Container.Value;
        var name = "net_" + Guid.NewGuid().ToString("N");

        using var connection = new NpgsqlConnection(container.GetConnectionString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{name}\"";
        command.ExecuteNonQuery();

        return new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = name }.ConnectionString;
    }
}

// The real API against a real Postgres with migrations applied. Seeding is on unless a subclass turns it off.
public class NetworkApiFactory : ApiFactory
{
    private readonly string _connectionString = SharedPostgres.CreateDatabase();

    protected virtual bool SeedReferenceData => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:smartraildb"] = _connectionString,
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["Network:Seed:ReferenceData"] = SeedReferenceData ? "true" : "false"
            });
        });
    }
}

public sealed class UnseededNetworkApiFactory : NetworkApiFactory
{
    protected override bool SeedReferenceData => false;
}
