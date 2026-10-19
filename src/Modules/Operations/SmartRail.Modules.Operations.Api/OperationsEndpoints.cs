using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartRail.Api.Core;
using SmartRail.Api.Core.Security;
using SmartRail.Modules.Operations.Application.Disruptions;

namespace SmartRail.Modules.Operations.Api;

public static class OperationsEndpoints
{
    // Disruptions change when a controller acts, so public reads are cached for a few seconds only (a retraction shows within 5 s).
    private const int MaxAgeSeconds = 5;

    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/operations").WithTags(Tags.Operations);

        group.MapGet("/disruptions", async (HttpContext http, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ListDisruptionsQuery(Language(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value, MaxAgeSeconds) : ApiResults.Problem(result);
            })
            .AllowAnonymous()
            .WithSummary("Disruptions passengers should know about");

        group.MapGet("/disruptions/{id:guid}", async (HttpContext http, Guid id, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDisruptionQuery(id, Language(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value, MaxAgeSeconds) : ApiResults.Problem(result);
            })
            .AllowAnonymous()
            .WithSummary("A disruption with the trains it affects");

        group.MapPost("/disruptions", async (ClaimsPrincipal user, CreateDisruptionRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateDisruptionCommand(
                    Actor(user), request.Title ?? string.Empty, request.Message ?? string.Empty, request.Severity ?? "Minor",
                    request.NetworkWide, request.StationCodes ?? [], request.ExpectedEndUtc, request.ReviewByUtc), ct);
                return result.IsSuccess ? Results.Created($"/api/operations/disruptions/{result.Value}", new { id = result.Value }) : ApiResults.Problem(result);
            })
            .RequireAuthorization(Policies.Controller)
            .WithSummary("Create a disruption (network-wide ones wait for a second approver)");

        group.MapPost("/disruptions/{id:guid}/approve", (ClaimsPrincipal user, Guid id, ISender sender, CancellationToken ct) =>
                Act(user, id, DisruptionAction.Approve, sender, ct))
            .RequireAuthorization(Policies.Controller)
            .WithSummary("Approve a network-wide disruption as a second person");

        group.MapPost("/disruptions/{id:guid}/resolve", (ClaimsPrincipal user, Guid id, ISender sender, CancellationToken ct) =>
                Act(user, id, DisruptionAction.Resolve, sender, ct))
            .RequireAuthorization(Policies.Controller)
            .WithSummary("Mark a disruption as over");

        group.MapPost("/disruptions/{id:guid}/retract", (ClaimsPrincipal user, Guid id, ISender sender, CancellationToken ct) =>
                Act(user, id, DisruptionAction.Retract, sender, ct))
            .RequireAuthorization(Policies.Controller)
            .WithSummary("Retract a disruption that was wrong");

        return app;
    }

    private static async Task<IResult> Act(ClaimsPrincipal user, Guid id, DisruptionAction action, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new ActOnDisruptionCommand(id, Actor(user), action), ct);
        return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
    }

    private static string Actor(ClaimsPrincipal user) => user.FindFirstValue("sub") ?? "unknown";

    private static string? Language(HttpContext http, string? lang)
    {
        if (!string.IsNullOrWhiteSpace(lang))
            return lang;

        var header = http.Request.Headers.AcceptLanguage.ToString();
        var first = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return first?.Split(';')[0].Split('-')[0];
    }
}

public sealed record CreateDisruptionRequest(
    string? Title, string? Message, string? Severity, bool NetworkWide, IReadOnlyList<string>? StationCodes,
    DateTimeOffset? ExpectedEndUtc, DateTimeOffset ReviewByUtc);
