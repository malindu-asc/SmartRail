using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SmartRail.Modules.Notifications.Api;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags(Tags.Notifications);

        // Map this module's endpoints on `group`: bind, send the command or query, map the Result.

        return app;
    }
}

// Request records go here.
