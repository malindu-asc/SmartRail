using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartRail.Api.Core;
using SmartRail.Api.Core.Security;
using SmartRail.Modules.Network.Application.Timetables.Versions;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Api;

// The scheduler's timetable workflow (UC-OPS-05, SPEC-015): draft from the version in force, edit, submit, a different person approves.
// Passenger-facing timetable reads stay under /api/network.
public static class TimetableVersionEndpoints
{
    public static IEndpointRouteBuilder MapTimetableVersionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ops/timetable/versions").WithTags("Timetable versions");

        group.MapGet("", async (ISender sender, CancellationToken ct) => Respond(await sender.Send(new ListTimetableVersionsQuery(), ct)))
            .RequirePermission(Permissions.TimetableRead)
            .WithSummary("Timetable versions: drafts and in review first, then published and superseded, with the one in force marked");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => Respond(await sender.Send(new GetTimetableVersionQuery(id), ct)))
            .RequirePermission(Permissions.TimetableRead)
            .WithSummary("One version with its services and stops, and the actions you may take (edit, submit, approve)");

        group.MapGet("/{id:guid}/compare", async (Guid id, Guid? against, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new CompareTimetableVersionQuery(id, against), ct)))
            .RequirePermission(Permissions.TimetableRead)
            .WithSummary("What changed against another version (default: the one in force today): added, removed and changed services");

        group.MapPost("", async (CreateDraftRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new CreateTimetableDraftCommand(r.Label ?? string.Empty), ct)))
            .RequirePermission(Permissions.TimetableDraft)
            .WithSummary("Start a draft as a copy of the version in force (one open draft at a time)");

        group.MapPost("/{id:guid}/services", async (Guid id, AddServiceRequest r, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new AddServiceCommand(
                    id, r.TrainNumber ?? string.Empty, r.RouteCode ?? string.Empty, r.CalendarCode ?? string.Empty, r.Direction ?? "Forward",
                    r.Category ?? "Regional", r.Stops ?? []), ct);
                return result.IsSuccess ? Results.Created($"/api/ops/timetable/versions/{id}", new { id = result.Value }) : ApiResults.Problem(result);
            })
            .RequirePermission(Permissions.TimetableDraft)
            .WithSummary("Add a service to a draft: route, days, direction, and a time at each stop");

        group.MapPut("/{id:guid}/services/{serviceId:guid}/times", async (Guid id, Guid serviceId, ChangeTimesRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new ChangeServiceTimesCommand(id, serviceId, r.Stops ?? []), ct)))
            .RequirePermission(Permissions.TimetableDraft)
            .WithSummary("Change the times of a service in a draft (the stops stay as they are)");

        group.MapDelete("/{id:guid}/services/{serviceId:guid}", async (Guid id, Guid serviceId, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new RemoveServiceCommand(id, serviceId), ct)))
            .RequirePermission(Permissions.TimetableDraft)
            .WithSummary("Remove a service from a draft");

        group.MapPost("/{id:guid}/submit", async (Guid id, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new SubmitTimetableVersionCommand(id), ct)))
            .RequirePermission(Permissions.TimetableDraft)
            .WithSummary("Send a draft for review");

        group.MapPost("/{id:guid}/approve", async (Guid id, ApproveRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new ApproveTimetableVersionCommand(id, r.EffectiveFrom), ct)))
            .RequirePermission(Permissions.TimetableApprove)
            .WithSummary("Approve a version in review as a different person, with the date it takes effect; the old version is superseded");

        return app;
    }

    private static IResult Respond<T>(Result<T> result) => result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    private static IResult NoContent(Result result) => result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
}

public sealed record CreateDraftRequest(string? Label);

public sealed record AddServiceRequest(
    string? TrainNumber, string? RouteCode, string? CalendarCode, string? Direction, string? Category, IReadOnlyList<StopTimeInput>? Stops);

public sealed record ChangeTimesRequest(IReadOnlyList<StopTimeInput>? Stops);

public sealed record ApproveRequest(DateOnly EffectiveFrom);
