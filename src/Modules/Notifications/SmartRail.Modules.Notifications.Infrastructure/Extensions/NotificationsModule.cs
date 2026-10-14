using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Modules.Notifications.Application.Abstractions;
using SmartRail.Modules.Notifications.Infrastructure.Persistence;
using SmartRail.Infrastructure.Core.Outbox;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Notifications.Infrastructure.Extensions;

public static class NotificationsModule
{
    public static Assembly ApplicationAssembly => typeof(Application.AssemblyReference).Assembly;

    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>((sp, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("smartraildb"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", NotificationsDbContext.Schema))
            .AddInterceptors(
                sp.GetRequiredService<AuditableEntitiesInterceptor>(),
                sp.GetRequiredService<OutboxMessagesInterceptor>()));

        services.AddScoped<INotificationsUnitOfWork>(sp => sp.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<NotificationsDbContext>>();

        // Register repositories here.

        return services;
    }
}
