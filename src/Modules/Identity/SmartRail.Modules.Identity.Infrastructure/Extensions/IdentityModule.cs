using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Identity.Infrastructure.Feeds;
using SmartRail.Modules.Identity.Infrastructure.Persistence;
using SmartRail.Infrastructure.Core.Outbox;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Identity.Infrastructure.Extensions;

public static class IdentityModule
{
    public static Assembly ApplicationAssembly => typeof(Application.AssemblyReference).Assembly;

    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>((sp, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("smartraildb"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema))
            .AddInterceptors(
                sp.GetRequiredService<AuditableEntitiesInterceptor>(),
                sp.GetRequiredService<OutboxMessagesInterceptor>()));

        services.AddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<IdentityDbContext>>();

        services.AddScoped<IFeedSources, FeedSources>();
        services.AddScoped<FeedSourceSeeder>();

        return services;
    }
}
