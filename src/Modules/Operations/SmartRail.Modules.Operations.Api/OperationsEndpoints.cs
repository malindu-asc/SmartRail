using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SmartRail.Modules.Operations.Api;

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/operations").WithTags(Tags.Operations);

        // Map this module's endpoints on `group`: bind, send the command or query, map the Result.

        return app;
    }
}

// Request records go here.
