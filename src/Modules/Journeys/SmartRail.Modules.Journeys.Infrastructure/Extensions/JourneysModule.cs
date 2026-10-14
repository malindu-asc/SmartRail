using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Infrastructure.Persistence;
using SmartRail.Infrastructure.Core.Outbox;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Journeys.Infrastructure.Extensions;

public static class JourneysModule
{
    public static Assembly ApplicationAssembly => typeof(Application.AssemblyReference).Assembly;

    public static IServiceCollection AddJourneysModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<JourneysDbContext>((sp, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("smartraildb"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", JourneysDbContext.Schema))
            .AddInterceptors(
                sp.GetRequiredService<AuditableEntitiesInterceptor>(),
                sp.GetRequiredService<OutboxMessagesInterceptor>()));

        services.AddScoped<IJourneysUnitOfWork>(sp => sp.GetRequiredService<JourneysDbContext>());
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<JourneysDbContext>>();

        // Register repositories here.

        return services;
    }
}
