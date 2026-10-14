using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SmartRail.Modules.Journeys.Api;

public static class JourneysEndpoints
{
    public static IEndpointRouteBuilder MapJourneysEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/journeys").WithTags(Tags.Journeys);

        // Map this module's endpoints on `group`: bind, send the command or query, map the Result.

        return app;
    }
}

// Request records go here.
