using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Identity.Api;
using SmartRail.Modules.Identity.Infrastructure.Feeds;
using SmartRail.Modules.Identity.Infrastructure.Persistence;
using SmartRail.Modules.Journeys.Api;
using SmartRail.Modules.Journeys.Infrastructure.Persistence;
using SmartRail.Modules.Network.Api;
using SmartRail.Modules.Network.Infrastructure.Persistence;
using SmartRail.Modules.Notifications.Api;
using SmartRail.Modules.Notifications.Infrastructure.Persistence;
using SmartRail.Modules.Operations.Api;
using SmartRail.Modules.Operations.Infrastructure.Persistence;
using SmartRail.Modules.Ticketing.Api;
using SmartRail.Modules.Ticketing.Infrastructure.Persistence;

namespace SmartRail.Api.Extensions;

// The host is the only place that knows every module.
internal static class WebApplicationExtensions
{
    internal static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapIdentityEndpoints();
        app.MapAdminEndpoints();
        app.MapNetworkEndpoints();
        app.MapJourneysEndpoints();
        app.MapOpsJourneysEndpoints();
        app.MapOperationsEndpoints();
        app.MapTimetableVersionEndpoints();
        app.MapNotificationsEndpoints();
        app.MapTicketingEndpoints();
        return app;
    }

    // Development only. Other environments apply migrations as an explicit step.
    internal static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        await services.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NetworkDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<JourneysDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<OperationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<TicketingDbContext>().Database.MigrateAsync();
    }

    // Development and test feed sources from configuration (Identity:FeedSources:Seed). Insert-only.
    internal static async Task SeedFeedSourcesAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FeedSourceSeeder>().SeedAsync();
    }
}
