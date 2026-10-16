using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Infrastructure.Persistence;
using SmartRail.Modules.Network.Infrastructure.Seeding;
using SmartRail.Infrastructure.Core.Outbox;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Network.Infrastructure.Extensions;

public static class NetworkModule
{
    public static Assembly ApplicationAssembly => typeof(Application.AssemblyReference).Assembly;

    public static IServiceCollection AddNetworkModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NetworkDbContext>((sp, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("smartraildb"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", NetworkDbContext.Schema))
            .AddInterceptors(
                sp.GetRequiredService<AuditableEntitiesInterceptor>(),
                sp.GetRequiredService<OutboxMessagesInterceptor>()));

        services.AddScoped<INetworkUnitOfWork>(sp => sp.GetRequiredService<NetworkDbContext>());
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<NetworkDbContext>>();
        services.AddScoped<INetworkReader, NetworkReader>();
        services.AddScoped<NetworkReferenceSeeder>();

        return services;
    }

    // Inserts the demo network when it is missing. Never updates existing rows.
    public static async Task SeedNetworkReferenceDataAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<NetworkReferenceSeeder>().SeedAsync(cancellationToken);
    }
}
