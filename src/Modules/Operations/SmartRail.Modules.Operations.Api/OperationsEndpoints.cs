using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartRail.Api.Core;
using SmartRail.Api.Core.Security;
using SmartRail.Modules.Operations.Application.Console;
using SmartRail.Modules.Operations.Application.Disruptions;
using SmartRail.Modules.Operations.Application.Incidents;
using SmartRail.Modules.Operations.Application.Messages;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Api;

// Passenger-facing reads (public), then the controller console: disruptions, incidents, driver messages, the dashboard and the audit
// log (SPEC-031). Every console endpoint asks for one permission (permission-matrix.md).
public static class OperationsEndpoints
{
    // Disruptions change when a controller acts, so public reads are cached for a few seconds only (a retraction shows within 5 s).
    private const int MaxAgeSeconds = 5;

    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        MapPublic(app);
        MapDisruptions(app);
        MapIncidents(app);
        MapMessages(app);
        MapDashboardAndAudit(app);
        MapDriver(app);
        return app;
    }

    private static void MapPublic(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/operations").WithTags(Tags.Operations);

        group.MapGet("/disruptions", async (HttpContext http, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ListDisruptionsQuery(Language(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value, MaxAgeSeconds) : ApiResults.Problem(result);
            })
            .AllowAnonymous()
            .WithSummary("Disruptions passengers should know about (published, including planned works)");

        group.MapGet("/disruptions/{id:guid}", async (HttpContext http, Guid id, string? lang, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDisruptionQuery(id, Language(http, lang)), ct);
                return result.IsSuccess ? PublicCaching.Ok(http, result.Value, MaxAgeSeconds) : ApiResults.Problem(result);
            })
            .AllowAnonymous()
            .WithSummary("A disruption with the trains it affects");
    }

    // ---- disruptions: draft, submit, second-person approval, resolve, retract (UC-OPS-04, REQ-OPS-005/006) ----

    private static void MapDisruptions(IEndpointRouteBuilder app)
    {
        var ops = app.MapGroup("/api/ops/disruptions").WithTags(Tags.Console);

        ops.MapGet("", async (string? tab, ISender sender, CancellationToken ct) => Respond(await sender.Send(new ListDisruptionsConsoleQuery(tab), ct)))
            .RequirePermission(Permissions.DisruptionCreate)
            .WithSummary("Console list: Active, Awaiting approval, Drafts, Resolved, with counts and an expired flag");

        ops.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => Respond(await sender.Send(new GetDisruptionConsoleQuery(id), ct)))
            .RequirePermission(Permissions.DisruptionCreate)
            .WithSummary("One disruption with its history, the actions its state allows and who can approve it");

        ops.MapPost("/preview", async (DisruptionPreviewRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new PreviewDisruptionQuery(r.Title ?? string.Empty, r.Message ?? string.Empty, r.Severity ?? "Minor"), ct)))
            .RequirePermission(Permissions.DisruptionCreate)
            .WithSummary("How the notice will look to passengers (plain text, never markup)");

        ops.MapPost("", async (DisruptionRequest r, ISender sender, CancellationToken ct) => Respond(await sender.Send(r.ToCommand(null, null), ct)))
            .RequirePermission(Permissions.DisruptionCreate)
            .WithSummary("Save a draft, or submit: a local notice publishes at once, a network-wide notice waits for a second person");

        ops.MapPut("/{id:guid}", async (Guid id, DisruptionRequest r, ISender sender, CancellationToken ct) => Respond(await sender.Send(r.ToCommand(id, null), ct)))
            .RequirePermission(Permissions.DisruptionCreate)
            .WithSummary("Change a draft, a rejected notice, or a live local disruption (a live network-wide notice cannot be edited)");

        ops.MapPost("/{id:guid}/submit", async (Guid id, ISender sender, CancellationToken ct) => NoContent(await sender.Send(new SubmitDisruptionCommand(id), ct)))
            .RequirePermission(Permissions.DisruptionCreate)
            .WithSummary("Send a saved draft (or a rejected notice) on its way");

        ops.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new ActOnDisruptionCommand(id, DisruptionAction.Approve, null), ct)))
            .RequirePermission(Permissions.DisruptionApproveNetworkWide)
            .WithSummary("Approve a network-wide notice as a second person (not its author)");

        ops.MapPost("/{id:guid}/reject", async (Guid id, CommentRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new RejectDisruptionCommand(id, r.Comment ?? string.Empty), ct)))
            .RequirePermission(Permissions.DisruptionApproveNetworkWide)
            .WithSummary("Send a network-wide notice back to its author with a comment");

        ops.MapPost("/{id:guid}/resolve", async (Guid id, ReasonRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new ActOnDisruptionCommand(id, DisruptionAction.Resolve, r.Reason), ct)))
            .RequirePermission(Permissions.DisruptionPublish)
            .WithSummary("Mark a disruption as over");

        ops.MapPost("/{id:guid}/retract", async (Guid id, ReasonRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new ActOnDisruptionCommand(id, DisruptionAction.Retract, r.Reason), ct)))
            .RequirePermission(Permissions.DisruptionRetract)
            .WithSummary("Retract a disruption that was wrong; it disappears from every passenger client");

        // The scheduler publishes planned works (a start time in the future). A network-wide one still needs a controller's approval.
        var planned = app.MapGroup("/api/ops/planned-works").WithTags(Tags.Console);

        planned.MapGet("", async (ISender sender, CancellationToken ct) => Respond(await sender.Send(new ListDisruptionsConsoleQuery("planned"), ct)))
            .RequirePermission(Permissions.DisruptionCreatePlanned)
            .WithSummary("Planned works that have not started yet");

        planned.MapPost("", async (DisruptionRequest r, ISender sender, CancellationToken ct) =>
                r.StartsAtUtc is null
                    ? Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Operations.StartRequired", detail: "Planned works need a start time.")
                    : Respond(await sender.Send(r.ToCommand(null, "Planned works"), ct)))
            .RequirePermission(Permissions.DisruptionCreatePlanned)
            .WithSummary("Announce planned works in advance (REQ-OPS-016)");
    }

    // ---- incidents (UC-OPS-07, REQ-OPS-007) ----

    private static void MapIncidents(IEndpointRouteBuilder app)
    {
        var ops = app.MapGroup("/api/ops/incidents").WithTags(Tags.Console);

        ops.MapGet("", async (bool? openOnly, ISender sender, CancellationToken ct) => Respond(await sender.Send(new ListIncidentsQuery(openOnly ?? false), ct)))
            .RequirePermission(Permissions.IncidentDeclare)
            .WithSummary("Incidents, newest first, with owner and severity");

        ops.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => Respond(await sender.Send(new GetIncidentQuery(id), ct)))
            .RequirePermission(Permissions.IncidentDeclare)
            .WithSummary("One incident with its timeline, linked disruption and messages");

        ops.MapGet("/sign-out-check", async (ISender sender, CancellationToken ct) => Respond(await sender.Send(new GetSignOutCheckQuery(), ct)))
            .RequirePermission(Permissions.IncidentDeclare)
            .WithSummary("What the signed-in controller still owns: an incident must be handed over before sign-out");

        ops.MapPost("", async (ClaimsPrincipal user, DeclareIncidentRequest r, ISender sender, CancellationToken ct) =>
            {
                // Only the Incident Manager capability can declare a Critical incident.
                if (string.Equals(r.Severity, "Critical", StringComparison.OrdinalIgnoreCase) && !Allows(user, Permissions.IncidentDeclareCritical))
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "You cannot declare a Critical incident.");

                return Respond(await sender.Send(new DeclareIncidentCommand(
                    r.Title ?? string.Empty, r.Severity ?? "Major", r.StationCodes ?? [], r.ReportedBy, r.DisruptionId, r.Confirm, r.Reason), ct));
            })
            .RequirePermission(Permissions.IncidentDeclare)
            .WithSummary("Declare an incident (you become its owner); Critical needs confirm and a reason");

        ops.MapPost("/{id:guid}/notes", async (Guid id, IncidentNoteRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new AddIncidentNoteCommand(id, r.Kind ?? "Note", r.Text ?? string.Empty, r.Detail), ct)))
            .RequirePermission(Permissions.IncidentDeclare)
            .WithSummary("Add a note or an action to the timeline");

        ops.MapPost("/{id:guid}/link-disruption", async (Guid id, LinkDisruptionRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new LinkIncidentDisruptionCommand(id, r.DisruptionId), ct)))
            .RequirePermission(Permissions.IncidentDeclare)
            .WithSummary("Link the passenger notice that goes with this incident");

        ops.MapPost("/{id:guid}/handover", async (Guid id, HandOverRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new HandOverIncidentCommand(id, r.NewOwnerSubject ?? string.Empty, r.Note ?? string.Empty), ct)))
            .RequirePermission(Permissions.IncidentReassignOwner)
            .WithSummary("Hand the incident to another controller with a note");

        ops.MapPost("/{id:guid}/resolve", async (Guid id, NoteRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new ResolveIncidentCommand(id, r.Note ?? string.Empty), ct)))
            .RequirePermission(Permissions.IncidentDeclare)
            .WithSummary("Close the incident with a resolution note");

        ops.MapPost("/{id:guid}/emergency-termination", async (Guid id, EmergencyTerminationRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new EmergencyTerminationCommand(id, r.JourneyId, r.Confirm, r.Reason ?? string.Empty), ct)))
            .RequirePermission(Permissions.JourneyTerminateEmergency)
            .WithSummary("Terminate a train in an emergency; passengers see only the approved wording (REQ-OPS-008)");
    }

    // ---- messages to drivers and assistance requests (UC-OPS-08, REQ-OPS-013) ----

    private static void MapMessages(IEndpointRouteBuilder app)
    {
        var ops = app.MapGroup("/api/ops").WithTags(Tags.Console);

        ops.MapGet("/message-templates", async (ISender sender, CancellationToken ct) => Respond(await sender.Send(new ListMessageTemplatesQuery(), ct)))
            .RequirePermission(Permissions.MessageSendToDriver)
            .WithSummary("Ready-made driver messages");

        ops.MapGet("/messages", async (bool? openOnly, ISender sender, CancellationToken ct) => Respond(await sender.Send(new ListMessagesQuery(openOnly ?? false), ct)))
            .RequirePermission(Permissions.MessageSendToDriver)
            .WithSummary("Today's driver messages with their state: Sent, Delivered, Acknowledged or not acknowledged");

        ops.MapGet("/messages/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => Respond(await sender.Send(new GetMessageQuery(id), ct)))
            .RequirePermission(Permissions.MessageSendToDriver)
            .WithSummary("One message and who has seen it");

        ops.MapPost("/messages", async (SendMessageRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new SendMessageCommand(
                    r.DriverSubject, r.JourneyId, r.IncidentId, r.Template, r.Body ?? string.Empty, r.RequiresAck, r.ValidUntilUtc), ct)))
            .RequirePermission(Permissions.MessageSendToDriver)
            .WithSummary("Send a plain-text message to a driver, or to a journey's driver");

        ops.MapPost("/messages/{id:guid}/resend", async (Guid id, ResendRequest r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new ResendMessageCommand(id, r.ValidForMinutes <= 0 ? 30 : r.ValidForMinutes), ct)))
            .RequirePermission(Permissions.MessageSendToDriver)
            .WithSummary("Send an unacknowledged message again");

        ops.MapGet("/assistance", async (ISender sender, CancellationToken ct) => Respond(await sender.Send(new ListAssistanceQuery(), ct)))
            .RequirePermission(Permissions.MessageSendToDriver)
            .WithSummary("Drivers asking for help, oldest first");

        ops.MapPost("/assistance/{id:guid}/handle", async (Guid id, OutcomeRequest r, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new HandleAssistanceCommand(id, r.Outcome), ct)))
            .RequirePermission(Permissions.MessageSendToDriver)
            .WithSummary("Mark an assistance request as handled");
    }

    // ---- dashboard, impact preview, audit log ----

    private static void MapDashboardAndAudit(IEndpointRouteBuilder app)
    {
        var ops = app.MapGroup("/api/ops").WithTags(Tags.Console);

        // Network-wide (devices, driver help requests, messages), so it is for the controller and the scheduler; a station master's own
        // station view is SPEC-028.
        ops.MapGet("/dashboard", async (ISender sender, CancellationToken ct) => Respond(await sender.Send(new GetOpsDashboardQuery(), ct)))
            .RequirePermission(Permissions.PlanningRead)
            .WithSummary("Network dashboard: running, on time, delayed, cancelled, what needs attention, device health, active disruptions");

        ops.MapGet("/journeys/{id:guid}/impact", async (Guid id, string action, int? stopSequence, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetImpactQuery(id, action, stopSequence), ct)))
            .RequirePermission(Permissions.PlanningRead)
            .WithSummary("Who is affected before a cancel, terminate-short, platform change or interrupt is confirmed");

        // One audit view over every module. The operational log needs audit.read.operational, the access log audit.read.access.
        app.MapGet("/api/audit", async (
                ClaimsPrincipal user, DateTimeOffset? from, DateTimeOffset? to, string? actor, string? action, string? q, string? kind, int? take,
                ISender sender, CancellationToken ct) =>
            {
                var operational = Allows(user, Permissions.AuditReadOperational);
                var access = Allows(user, Permissions.AuditReadAccess);

                // kind narrows the view to one log; asking for a log you may not read is forbidden, not silently empty.
                if (string.Equals(kind, "operational", StringComparison.OrdinalIgnoreCase))
                    access = false;
                else if (string.Equals(kind, "access", StringComparison.OrdinalIgnoreCase))
                    operational = false;

                if (!operational && !access)
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "You cannot read this audit log.");

                return Respond(await sender.Send(new ListAuditQuery(operational, access, from, to, actor, action, q, take ?? 100), ct));
            })
            .RequireAuthorization()
            .WithTags(Tags.Console)
            .WithSummary("Audit log (append-only): who did what, to what, why. Controller: operational and access; administrator: access only");
    }

    // ---- the driver's side ----

    private static void MapDriver(IEndpointRouteBuilder app)
    {
        var driver = app.MapGroup("/api/driver").WithTags("Driver app");

        driver.MapGet("/messages", async (ISender sender, CancellationToken ct) => Respond(await sender.Send(new ListMyMessagesQuery(), ct)))
            .RequirePermission(Permissions.MessageAcknowledge)
            .WithSummary("The driver's own messages from the controller (fetching marks them delivered)");

        driver.MapPost("/messages/{id:guid}/acknowledge", async (Guid id, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new AcknowledgeMessageCommand(id), ct)))
            .RequirePermission(Permissions.MessageAcknowledge)
            .WithSummary("Acknowledge a message (another driver's message looks not found)");

        driver.MapPost("/assistance", async (AssistanceRequestBody r, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new RequestAssistanceCommand(r.JourneyId, r.StationCode, r.Note), ct)))
            .RequirePermission(Permissions.AssistanceRequest)
            .WithSummary("Ask the controller for help; it shows on the controller's dashboard");
    }

    private static bool Allows(ClaimsPrincipal user, string permission) =>
        RolePermissions.Allows(user.FindAll("roles").Select(c => c.Value), permission);

    private static IResult Respond<T>(Result<T> result) => result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    private static IResult NoContent(Result result) => result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);

    private static string? Language(HttpContext http, string? lang)
    {
        if (!string.IsNullOrWhiteSpace(lang))
            return lang;

        var header = http.Request.Headers.AcceptLanguage.ToString();
        var first = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return first?.Split(';')[0].Split('-')[0];
    }
}

public sealed record DisruptionRequest(
    string? Title, string? Message, string? Severity, bool NetworkWide, IReadOnlyList<string>? StationCodes, string? Type,
    DateTimeOffset? StartsAtUtc, DateTimeOffset? ExpectedEndUtc, DateTimeOffset ReviewByUtc, bool Submit)
{
    internal SaveDisruptionCommand ToCommand(Guid? id, string? defaultType) => new(
        id, Title ?? string.Empty, Message ?? string.Empty, Severity ?? "Minor", NetworkWide, StationCodes ?? [], Type ?? defaultType,
        StartsAtUtc, ExpectedEndUtc, ReviewByUtc, Submit);
}

public sealed record DisruptionPreviewRequest(string? Title, string? Message, string? Severity);

public sealed record CommentRequest(string? Comment);

public sealed record ReasonRequest(string? Reason);

public sealed record NoteRequest(string? Note);

public sealed record OutcomeRequest(string? Outcome);

public sealed record DeclareIncidentRequest(
    string? Title, string? Severity, IReadOnlyList<string>? StationCodes, string? ReportedBy, Guid? DisruptionId, bool Confirm, string? Reason);

public sealed record IncidentNoteRequest(string? Kind, string? Text, string? Detail);

public sealed record LinkDisruptionRequest(Guid DisruptionId);

public sealed record HandOverRequest(string? NewOwnerSubject, string? Note);

public sealed record EmergencyTerminationRequest(Guid JourneyId, bool Confirm, string? Reason);

public sealed record SendMessageRequest(
    string? DriverSubject, Guid? JourneyId, Guid? IncidentId, string? Template, string? Body, bool RequiresAck, DateTimeOffset ValidUntilUtc);

public sealed record ResendRequest(int ValidForMinutes);

public sealed record AssistanceRequestBody(Guid? JourneyId, string? StationCode, string? Note);
