using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SmartRail.Modules.Identity.Api;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity").WithTags(Tags.Identity);

        // Map this module's endpoints on `group`: bind, send the command or query, map the Result.

        return app;
    }
}

// Request records go here.
