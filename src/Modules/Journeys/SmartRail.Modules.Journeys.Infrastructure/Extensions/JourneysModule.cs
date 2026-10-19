using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Infrastructure.Core.Outbox;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Contracts;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Journeys.Domain.Live;
using SmartRail.Modules.Journeys.Infrastructure.Generation;
using SmartRail.Modules.Journeys.Infrastructure.Persistence;
using SmartRail.Modules.Journeys.Infrastructure.Seeding;

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

        services.AddScoped<IJourneyRepository, JourneyRepository>();
        services.AddScoped<JourneyReader>();
        services.AddScoped<IJourneyReader>(sp => sp.GetRequiredService<JourneyReader>());
        services.AddScoped<IJourneyLookup>(sp => sp.GetRequiredService<JourneyReader>());
        services.AddScoped<IJourneySearch, SmartRail.Modules.Journeys.Application.Queries.Search.JourneySearch>();
        services.AddScoped<IJourneysAuditLog, JourneysAuditLog>();
        services.AddScoped<IIngestStore, IngestStore>();

        // The numbers behind live data (Journeys:Live): stale and unknown thresholds, dwell, speed ceiling.
        services.AddSingleton(configuration.GetSection("Journeys:Live").Get<LiveRules>() ?? LiveRules.Default);

        services.Configure<JourneyGenerationOptions>(configuration.GetSection(JourneyGenerationOptions.SectionName));
        services.AddScoped<JourneyGenerationRunner>();
        services.AddScoped<DemoFleetSeeder>();
        services.AddHostedService<JourneyGenerationWorker>();

        return services;
    }
}
