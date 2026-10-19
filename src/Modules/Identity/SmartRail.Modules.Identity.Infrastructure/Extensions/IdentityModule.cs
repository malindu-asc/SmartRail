using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Contracts;
using Microsoft.Extensions.Options;
using SmartRail.Modules.Identity.Infrastructure.Accounts;
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

        // The account directory (Keycloak). Without its own setting the address is taken from the token authority.
        services.Configure<KeycloakAdminOptions>(o =>
        {
            configuration.GetSection(KeycloakAdminOptions.Section).Bind(o);
            if (string.IsNullOrWhiteSpace(o.BaseUrl))
            {
                var authority = configuration["Authentication:Authority"] ?? string.Empty;
                var at = authority.IndexOf("/realms/", StringComparison.Ordinal);
                if (at > 0)
                {
                    o.BaseUrl = authority[..at];
                    o.Realm = authority[(at + "/realms/".Length)..].Trim('/');
                }
            }

            o.BaseUrl = o.BaseUrl.TrimEnd('/');
        });
        services.AddHttpClient<IAccountDirectory, KeycloakAccountDirectory>(client => client.Timeout = TimeSpan.FromSeconds(10));

        return services;
    }
}
