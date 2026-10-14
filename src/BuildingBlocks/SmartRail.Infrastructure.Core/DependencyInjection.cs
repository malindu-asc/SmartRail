using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Core.Events;
using SmartRail.Infrastructure.Core.EventBus;
using SmartRail.Infrastructure.Core.Outbox;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Infrastructure.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<OutboxMessagesInterceptor>();
        services.AddSingleton<AuditableEntitiesInterceptor>();
        services.AddScoped<IEventBus, InProcessEventBus>();

        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.AddHostedService<OutboxWorker>();

        return services;
    }
}
