using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Api.Core.Security;

namespace SmartRail.Modules.Identity.Api;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        // Diagnostics are off unless explicitly enabled (Compose, Aspire and tests turn them on).
        var configuration = app.ServiceProvider.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue<bool>("Diagnostics:Identity"))
            return app;

        var group = app.MapGroup("/api/identity").WithTags(Tags.Identity);

        // Who does the API think I am? Never returns the token itself.
        group.MapGet("/whoami", (ClaimsPrincipal user) => Results.Ok(new WhoAmIResponse(
                user.FindFirstValue("sub"),
                user.FindFirstValue("preferred_username"),
                user.FindAll("roles").Select(c => c.Value).Order().ToArray())))
            .RequireAuthorization()
            .WithSummary("Identity of the caller");

        // 200 when the caller satisfies the policy, 403 when not, 404 for an unknown policy.
        group.MapGet("/policy-check/{policy}", async (string policy, ClaimsPrincipal user, IAuthorizationService authorization) =>
            {
                if (!Policies.All.Contains(policy, StringComparer.Ordinal))
                    return Results.NotFound();

                var result = await authorization.AuthorizeAsync(user, policy);
                return result.Succeeded ? Results.Ok(new { policy, allowed = true }) : Results.Forbid();
            })
            .RequireAuthorization()
            .WithSummary("Check the caller against a named policy");

        return app;
    }
}

public sealed record WhoAmIResponse(string? Subject, string? UserName, IReadOnlyList<string> Roles);
