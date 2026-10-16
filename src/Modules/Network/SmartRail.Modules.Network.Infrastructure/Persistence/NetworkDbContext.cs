using Microsoft.EntityFrameworkCore;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Domain.Languages;
using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Stations;

namespace SmartRail.Modules.Network.Infrastructure.Persistence;

public sealed class NetworkDbContext(DbContextOptions<NetworkDbContext> options)
    : BaseDbContext(options), INetworkUnitOfWork
{
    public const string Schema = "network";

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Route> Routes => Set<Route>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NetworkDbContext).Assembly);

        // Network tables use snake_case column names (the outbox table owned by the base context is left alone).
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.ClrType.Namespace?.StartsWith("SmartRail.Modules.Network", StringComparison.Ordinal) != true)
                continue;

            foreach (var property in entity.GetProperties())
            {
                if (property.GetColumnName() is { } name && name == property.Name)
                    property.SetColumnName(ToSnakeCase(name));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        var builder = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0 && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                builder.Append('_');
            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
