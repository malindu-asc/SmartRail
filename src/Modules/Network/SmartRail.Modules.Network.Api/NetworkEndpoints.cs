using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SmartRail.Modules.Network.Api;

public static class NetworkEndpoints
{
    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/network").WithTags(Tags.Network);

        // Map this module's endpoints on `group`: bind, send the command or query, map the Result.

        return app;
    }
}

// Request records go here.
