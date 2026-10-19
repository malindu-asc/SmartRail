using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartRail.Api.Core;
using SmartRail.Api.Core.Security;
using SmartRail.Modules.Journeys.Application.Ops;

namespace SmartRail.Modules.Journeys.Api;

// The controller and scheduler consoles (SPEC-031). Every endpoint asks for one permission (permission-matrix.md); a station master
// only ever sees their own stations. Actions carry the version the screen was looking at, so two people cannot act on one old view
// (REQ-OPS-010): a stale version answers 409 with the current version.
public static class OpsJourneysEndpoints
{
    public static IEndpointRouteBuilder MapOpsJourneysEndpoints(this IEndpointRouteBuilder app)
    {
        var ops = app.MapGroup("/api/ops").WithTags("Operations console");

        // ---- live board and journey detail ----

        ops.MapGet("/journeys", async (ClaimsPrincipal user, DateOnly? date, string? route, string? station, string? status, bool? attention, string? q, ISender sender, CancellationToken ct) =>
            {
                var scope = StationScope.Resolve(user, station);
                if (scope.Forbidden)
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "A station master sees only their own stations.");

                return Respond(await sender.Send(new GetOpsBoardQuery(date, route, scope.Station, status, attention ?? false, q), ct));
            })
            .RequirePermission(Permissions.OpsRead)
            .WithSummary("Live board: today's journeys with state, delay, data freshness, driver, unit and what needs attention (filterable)");

        ops.MapGet("/journeys/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetOpsJourneyQuery(id), ct)))
            .RequirePermission(Permissions.OpsRead)
            .WithSummary("One journey: stops (scheduled, actual or expected, platform, source), crew and unit, and the actions its state allows");

        ops.MapGet("/journeys/{id:guid}/events", async (Guid id, int? take, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetOpsJourneyEventsQuery(id, take ?? 50), ct)))
            .RequirePermission(Permissions.OpsRead)
            .WithSummary("Event history of a journey, with where each event came from (train feed, driver, controller)");

        ops.MapGet("/reason-codes", async (ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetReasonCodesQuery(), ct)))
            .RequirePermission(Permissions.OpsRead)
            .WithSummary("The reasons a controller can pick for cancel, terminate short, interrupt and platform changes");

        // ---- controller actions (each needs a reason and writes an audit row) ----

        ops.MapPost("/journeys/{id:guid}/cancel", async (Guid id, CancelRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new CancelJourneyCommand(id, r.ReasonCode ?? string.Empty, r.Note, r.PassengerMessage, r.ExpectedVersion), ct)))
            .RequirePermission(Permissions.JourneyCancel)
            .WithSummary("Cancel a journey that has not left (or an interrupted one); its tickets become Affected");

        ops.MapPost("/journeys/{id:guid}/terminate-short", async (Guid id, TerminateShortRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new TerminateShortCommand(id, r.StopSequence, r.ReasonCode ?? string.Empty, r.Note, r.ExpectedVersion), ct)))
            .RequirePermission(Permissions.JourneyTerminateShort)
            .WithSummary("End a running journey at a stop it has not passed; later stops are cancelled");

        ops.MapPost("/journeys/{id:guid}/interrupt", async (Guid id, InterruptRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new InterruptJourneyCommand(id, r.ReasonCode ?? string.Empty, r.Note, r.ExpectedVersion), ct)))
            .RequirePermission(Permissions.JourneyInterrupt)
            .WithSummary("Interrupt a running journey: no estimate while it is interrupted");

        ops.MapPost("/journeys/{id:guid}/resume", async (Guid id, VersionRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new ResumeJourneyCommand(id, r.ExpectedVersion), ct)))
            .RequirePermission(Permissions.JourneyInterrupt)
            .WithSummary("Resume an interrupted journey");

        ops.MapPost("/journeys/{id:guid}/reinstate", async (Guid id, ReinstateRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new ReinstateJourneyCommand(id, r.Reason ?? string.Empty, r.ExpectedVersion), ct)))
            .RequirePermission(Permissions.JourneyReinstate)
            .WithSummary("Reinstate a cancelled journey before its scheduled departure");

        ops.MapGet("/journeys/{id:guid}/platform-check", async (Guid id, int stopSequence, string platform, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new CheckPlatformQuery(id, stopSequence, platform), ct)))
            .RequirePermission(Permissions.JourneyPlatformChange)
            .WithSummary("Is this platform free? Lists the trains that would share it");

        ops.MapPost("/journeys/{id:guid}/platform", async (Guid id, PlatformRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new ChangePlatformCommand(id, r.StopSequence, r.Platform ?? string.Empty, r.ReasonCode ?? string.Empty, r.Note, r.ConfirmOverlap, r.ExpectedVersion), ct)))
            .RequirePermission(Permissions.JourneyPlatformChange)
            .WithSummary("Change a platform; the first try is refused if another train is there, unless confirmOverlap is set");

        // ---- routes ----

        ops.MapGet("/routes", async (DateOnly? date, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetOpsRoutesQuery(date), ct)))
            .RequirePermission(Permissions.OpsRead)
            .WithSummary("Routes (one per direction) with the worst state of their journeys; no live data is never shown as on time");

        ops.MapGet("/routes/{key}", async (string key, DateOnly? date, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetOpsRouteQuery(key, date), ct)))
            .RequirePermission(Permissions.PlanningRead)
            .WithSummary("One route for a day: each journey with its driver, unit and assignment status; e.g. CEN-RIV");

        // ---- planning ----

        ops.MapGet("/planning/needs", async (DateOnly? date, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetPlanningNeedsQuery(date), ct)))
            .RequirePermission(Permissions.PlanningRead)
            .WithSummary("Journeys that need a driver or unit, with suggestions, and units that are going out of service");

        ops.MapGet("/planning/assignments", async (DateOnly? date, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetPlanningAssignmentsQuery(date), ct)))
            .RequirePermission(Permissions.PlanningRead)
            .WithSummary("Assignments for a day by unit: driver, journey times and status");

        ops.MapGet("/units", async (DateOnly? date, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new ListUnitsQuery(date), ct)))
            .RequirePermission(Permissions.PlanningRead)
            .WithSummary("Units with status (Available, OutOfService) and journeys today");

        ops.MapGet("/crew", async (DateOnly? date, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new ListCrewQuery(date), ct)))
            .RequirePermission(Permissions.PlanningRead)
            .WithSummary("Driver duties for a day");

        ops.MapGet("/journeys/{id:guid}/assignment-options", async (Guid id, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetAssignmentOptionsQuery(id), ct)))
            .RequirePermission(Permissions.PlanningRead)
            .WithSummary("Drivers and units that could take this journey, each with the checks (rest, overlap, availability)");

        ops.MapPost("/journeys/{id:guid}/assign", async (Guid id, AssignRequest r, ClaimsPrincipal user, IAuthorizationService authorization, ISender sender, CancellationToken ct) =>
            {
                // A unit needs the unit permission and a driver the driver permission; the scheduler holds both.
                if (r.UnitId is not null && !(await authorization.AuthorizeAsync(user, Permissions.JourneyVehicleAssign)).Succeeded)
                    return Results.Forbid();
                if (r.DutyId is not null && !(await authorization.AuthorizeAsync(user, Permissions.JourneyDriverAssign)).Succeeded)
                    return Results.Forbid();

                return Respond(await sender.Send(new AssignResourcesCommand(id, r.UnitId, r.DutyId, r.Reason ?? string.Empty, r.AcceptRestConflict, r.ExpectedVersion), ct));
            })
            .RequireAuthorization()
            .WithSummary("Assign a unit and/or a driver duty (with a reason); checks overlap, rest and availability");

        ops.MapPost("/journeys/{id:guid}/unassign", async (Guid id, UnassignRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new UnassignResourcesCommand(id, r.Unit, r.Driver, r.Reason ?? string.Empty, r.ExpectedVersion), ct)))
            .RequirePermission(Permissions.JourneyDriverAssign)
            .WithSummary("Take the unit and/or driver off a journey");

        ops.MapPost("/duties", async (CreateDutyRequest r, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateDutyCommand(r.DriverSubject ?? string.Empty, r.FromUtc, r.ToUtc), ct);
                return result.IsSuccess ? Results.Created($"/api/ops/crew", result.Value) : ApiResults.Problem(result);
            })
            .RequirePermission(Permissions.JourneyDriverAssign)
            .WithSummary("Roster a driver for a shift, so they can be assigned");

        ops.MapGet("/units/{id:guid}/impact", async (Guid id, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetUnitImpactQuery(id), ct)))
            .RequirePermission(Permissions.PlanningRead)
            .WithSummary("A unit that is out of service: the journeys that lose it and who could take over");

        ops.MapPost("/units/{id:guid}/out-of-service", async (Guid id, OutOfServiceRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new MarkUnitOutOfServiceCommand(id, r.FromUtc, r.ToUtc, r.Reason ?? string.Empty), ct)))
            .RequirePermission(Permissions.VehicleAvailabilitySet)
            .WithSummary("Mark a unit out of service for a period; returns the affected journeys and suggestions");

        ops.MapDelete("/units/{id:guid}/out-of-service/{periodId:guid}", async (Guid id, Guid periodId, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new EndUnitOutOfServiceCommand(id, periodId), ct)))
            .RequirePermission(Permissions.VehicleAvailabilitySet)
            .WithSummary("The unit is back in service");

        ops.MapPost("/units/{id:guid}/apply-suggestions", async (Guid id, ApplySuggestionsRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new ApplyUnitSuggestionsCommand(id,
                    r.Assignments?.Select(a => new Reassignment(a.JourneyId, a.UnitId)).ToList(), r.Reason ?? string.Empty), ct)))
            .RequirePermission(Permissions.JourneyVehicleAssign)
            .WithSummary("Apply the suggested replacement units (all, or the ones listed), one audited change each");

        // ---- reports ----

        ops.MapGet("/reports/punctuality", async (DateOnly? from, DateOnly? to, string? route, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetPunctualityReportQuery(from, to, route), ct)))
            .RequirePermission(Permissions.ReportRead)
            .WithSummary("On-time percentage by day and route, average delay, cancellations and corrected records");

        ops.MapGet("/reports/punctuality.csv", async (DateOnly? from, DateOnly? to, string? route, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ExportPunctualityCsvQuery(from, to, route), ct);
                return result.IsSuccess
                    ? Results.File(Encoding.UTF8.GetBytes(result.Value.Content), "text/csv", result.Value.FileName)
                    : ApiResults.Problem(result);
            })
            .RequirePermission(Permissions.ReportRead)
            .WithSummary("The punctuality report as CSV");

        return app;
    }

    private static IResult Respond<T>(SmartRail.SharedKernel.Results.Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    private static IResult NoContent(SmartRail.SharedKernel.Results.Result result) =>
        result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
}

// A station master works on their own stations only (from the "stations" claim). Controllers and schedulers see the network.
internal static class StationScope
{
    public sealed record Scope(string? Station, bool Forbidden);

    public static Scope Resolve(ClaimsPrincipal user, string? requested)
    {
        var roles = user.FindAll("roles").Select(c => c.Value).ToList();
        var wide = roles.Contains(Roles.Controller) || roles.Contains(Roles.Scheduler) || roles.Contains(Roles.Admin);
        if (wide || !roles.Contains(Roles.StationMaster))
            return new Scope(requested, false);

        var own = user.FindAll("stations").Select(c => c.Value.ToUpperInvariant()).ToList();
        var wanted = string.IsNullOrWhiteSpace(requested) ? own.FirstOrDefault() : requested.Trim().ToUpperInvariant();
        return wanted is not null && own.Contains(wanted) ? new Scope(wanted, false) : new Scope(null, true);
    }
}

public sealed record VersionRequest(int? ExpectedVersion);

public sealed record CancelRequest(string? ReasonCode, string? Note, string? PassengerMessage, int? ExpectedVersion);

public sealed record TerminateShortRequest(int StopSequence, string? ReasonCode, string? Note, int? ExpectedVersion);

public sealed record InterruptRequest(string? ReasonCode, string? Note, int? ExpectedVersion);

public sealed record ReinstateRequest(string? Reason, int? ExpectedVersion);

public sealed record PlatformRequest(int StopSequence, string? Platform, string? ReasonCode, string? Note, bool ConfirmOverlap, int? ExpectedVersion);

public sealed record AssignRequest(Guid? UnitId, Guid? DutyId, string? Reason, bool AcceptRestConflict, int? ExpectedVersion);

public sealed record UnassignRequest(bool Unit, bool Driver, string? Reason, int? ExpectedVersion);

public sealed record CreateDutyRequest(string? DriverSubject, DateTimeOffset FromUtc, DateTimeOffset ToUtc);

public sealed record OutOfServiceRequest(DateTimeOffset FromUtc, DateTimeOffset? ToUtc, string? Reason);

public sealed record ApplySuggestionsRequest(IReadOnlyList<SuggestedAssignment>? Assignments, string? Reason);

public sealed record SuggestedAssignment(Guid JourneyId, Guid UnitId);
