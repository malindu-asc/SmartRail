using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using SmartRail.Api.Core;
using SmartRail.Api.Core.Security;
using SmartRail.Modules.Identity.Application.Accounts;

namespace SmartRail.Modules.Identity.Api;

public static class IdentityEndpoints
{
    // Passenger accounts: create, forgot password, my profile, download my data, delete my data (SPEC-021).
    private static void MapAccounts(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account").WithTags(Tags.Identity);

        group.MapPost("/register", async (RegisterRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RegisterAccountCommand(request.FullName ?? string.Empty, request.Email ?? string.Empty, request.Password ?? string.Empty, request.AcceptedTerms), ct);
                return result.IsSuccess ? Results.Created("/api/account/me", result.Value) : ApiResults.Problem(result);
            })
            .AllowAnonymous()
            .WithSummary("Create a passenger profile");

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ForgotPasswordCommand(request.Email ?? string.Empty), ct);
                return Results.Accepted();
            })
            .AllowAnonymous()
            .WithSummary("Send a password reset link, without saying whether the email has an account");

        group.MapGet("/me", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyProfileQuery(AccountId(user)), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .RequireAuthorization()
            .WithSummary("My profile");

        group.MapGet("/me/export", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ExportMyDataQuery(AccountId(user)), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .RequireAuthorization()
            .WithSummary("Download my data");

        group.MapDelete("/me", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DeleteMyAccountCommand(AccountId(user)), ct);
                return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
            })
            .RequireAuthorization()
            .WithSummary("Delete my account and personal data");
    }

    // What the signed-in person may do. The consoles read this once after sign-in and show only what is allowed; the API still
    // checks every request (the screen is a convenience, never the control).
    private static void MapSession(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", (ClaimsPrincipal user) =>
            {
                var roles = user.FindAll("roles").Select(c => c.Value).Where(r => Roles.All.Contains(r)).Order().ToArray();
                var permissions = RolePermissions.For(roles).Order().ToArray();
                var consoles = new[] { Roles.Controller, Roles.Scheduler, Roles.Admin, Roles.StationMaster, Roles.Driver }
                    .Where(r => roles.Contains(r)).ToArray();

                return Results.Ok(new SessionResponse(
                    user.FindFirstValue("sub"),
                    user.FindFirstValue("preferred_username"),
                    user.FindFirstValue("name"),
                    roles,
                    permissions,
                    consoles));
            })
            .RequireAuthorization()
            .WithTags(Tags.Identity)
            .WithSummary("Who I am, my roles and every permission they give me");

        app.MapGet("/api/me/permissions/{permission}", (string permission, ClaimsPrincipal user) =>
                Permissions.All.Contains(permission, StringComparer.Ordinal)
                    ? Results.Ok(new { permission, allowed = RolePermissions.Allows(user.FindAll("roles").Select(c => c.Value), permission) })
                    : Results.NotFound())
            .RequireAuthorization()
            .WithTags(Tags.Identity)
            .WithSummary("Ask whether I hold one permission");
    }

    private static string AccountId(ClaimsPrincipal user) => user.FindFirstValue("sub") ?? string.Empty;

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        MapAccounts(app);
        MapSession(app);

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

public sealed record RegisterRequest(string? FullName, string? Email, string? Password, bool AcceptedTerms);

public sealed record ForgotPasswordRequest(string? Email);

public sealed record WhoAmIResponse(string? Subject, string? UserName, IReadOnlyList<string> Roles);

public sealed record SessionResponse(
    string? Subject,
    string? UserName,
    string? Name,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Consoles);
