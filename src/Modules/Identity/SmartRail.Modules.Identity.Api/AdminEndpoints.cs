using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartRail.Api.Core;
using SmartRail.Api.Core.Security;
using SmartRail.Modules.Identity.Application.Admin;
using SmartRail.Modules.Identity.Domain.Settings;

namespace SmartRail.Modules.Identity.Api;

// The System Administrator's console: people, roles, devices and security. None of it can run a train (REQ-OPS-001).
// Every request is checked here by permission; the console only hides what the caller cannot do.
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").WithTags("Admin");

        admin.MapGet("/dashboard", async (ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetAdminDashboardQuery(), ct)))
            .RequirePermission(Permissions.UserManage)
            .WithSummary("Admin dashboard: staff, devices, failed sign-ins, access review and what needs attention");

        // ---- users ----

        admin.MapGet("/users", async (string? role, string? status, string? q, string? station, int? skip, int? take, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new ListStaffQuery(role, status, q, station, skip ?? 0, take ?? 50), ct)))
            .RequirePermission(Permissions.UserManage)
            .WithSummary("Staff accounts, with role, status (Active, Locked, Disabled, Leaver), failed sign-ins and last sign-in");

        admin.MapGet("/users/{id}", async (string id, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetStaffQuery(id), ct)))
            .RequirePermission(Permissions.UserManage)
            .WithSummary("One staff account");

        admin.MapPost("/users", async (InviteStaffRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new InviteStaffCommand(request.FullName ?? string.Empty, request.Email ?? string.Empty,
                    request.Role ?? string.Empty, request.StationCodes ?? []), ct);
                return result.IsSuccess ? Results.Created($"/api/admin/users/{result.Value.Account.Id}", result.Value) : ApiResults.Problem(result);
            })
            .RequirePermission(Permissions.RoleAssign)
            .WithSummary("Invite a staff member with a role; they set their own password and two-step code");

        admin.MapPatch("/users/{id}", async (string id, UpdateStaffRequest request, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new UpdateStaffCommand(id, request.FullName, request.Email, request.Role, request.StationCodes, request.Reason), ct)))
            .RequirePermission(Permissions.UserManage)
            .WithSummary("Change name, email, stations or (with a reason) role");

        admin.MapPost("/users/{id}/disable", async (string id, ReasonRequest request, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new SetStaffEnabledCommand(id, false, request.Reason), ct)))
            .RequirePermission(Permissions.UserManage)
            .WithSummary("Disable access when someone leaves or changes job; their sign-in stops at once");

        admin.MapPost("/users/{id}/enable", async (string id, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new SetStaffEnabledCommand(id, true, null), ct)))
            .RequirePermission(Permissions.UserManage)
            .WithSummary("Enable a disabled account");

        admin.MapPost("/users/{id}/unlock", async (string id, ReasonRequest request, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new UnlockStaffCommand(id, request.Reason), ct)))
            .RequirePermission(Permissions.UserManage)
            .WithSummary("Unlock an account locked by failed sign-ins");

        // ---- roles and permissions (read only: changes go through a reviewed release) ----

        admin.MapGet("/roles", () => Results.Ok(RoleMatrix.Build()))
            .RequirePermission(Permissions.UserManage)
            .WithSummary("The roles and permissions matrix");

        // ---- devices ----

        // A station master reads the health of their own station's devices only.
        admin.MapGet("/devices", async (ClaimsPrincipal user, string? type, string? health, string? station, string? q, ISender sender, CancellationToken ct) =>
            {
                var scope = StationScope.Resolve(user, station);
                if (scope.Forbidden)
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "A station master sees only their own stations.");

                return Respond(await sender.Send(new ListDevicesQuery(type, health, scope.Station, q), ct));
            })
            .RequirePermission(Permissions.DeviceHealthRead)
            .WithSummary("Displays, gates and terminals with last heartbeat and health; ?type=displays|gates|terminals");

        admin.MapGet("/devices/{id:guid}", async (ClaimsPrincipal user, Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDeviceQuery(id), ct);
                if (result.IsSuccess && !StationScope.Allows(user, result.Value.StationCode))
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "A station master sees only their own stations.");

                return Respond(result);
            })
            .RequirePermission(Permissions.DeviceHealthRead)
            .WithSummary("One device");

        admin.MapPost("/devices", async (RegisterDeviceRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RegisterDeviceCommand(request.Name ?? string.Empty, request.Type ?? string.Empty,
                    request.StationCode ?? string.Empty, request.Location), ct);
                return result.IsSuccess ? Results.Created($"/api/admin/devices/{result.Value.Id}", result.Value) : ApiResults.Problem(result);
            })
            .RequirePermission(Permissions.DeviceProvision)
            .WithSummary("Register a display, gate scanner or terminal");

        admin.MapPost("/devices/{id:guid}/revoke", async (Guid id, ReasonRequest request, ISender sender, CancellationToken ct) =>
                NoContent(await sender.Send(new RevokeDeviceCommand(id, request.Reason ?? string.Empty), ct)))
            .RequirePermission(Permissions.DeviceRevoke)
            .WithSummary("Revoke a device: it is taken off the register (the record stays for the audit trail)");

        // ---- security ----

        admin.MapGet("/security", async (ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new GetSecuritySettingsQuery(), ct)))
            .RequirePermission(Permissions.SecuritySettingsManage)
            .WithSummary("Sign-in protection, sessions, access reviews and device thresholds");

        admin.MapPut("/security", async (SecuritySettings settings, ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new UpdateSecuritySettingsCommand(settings), ct)))
            .RequirePermission(Permissions.SecuritySettingsManage)
            .WithSummary("Change the security settings (the account-lock numbers are applied to the sign-in service)");

        admin.MapPost("/security/access-review/complete", async (ISender sender, CancellationToken ct) =>
                Respond(await sender.Send(new CompleteAccessReviewCommand(), ct)))
            .RequirePermission(Permissions.SecuritySettingsManage)
            .WithSummary("Record that every account was reviewed");

        return app;
    }

    private static IResult Respond<T>(SmartRail.SharedKernel.Results.Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    private static IResult NoContent(SmartRail.SharedKernel.Results.Result result) =>
        result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
}

public sealed record InviteStaffRequest(string? FullName, string? Email, string? Role, IReadOnlyList<string>? StationCodes);

public sealed record UpdateStaffRequest(string? FullName, string? Email, string? Role, IReadOnlyList<string>? StationCodes, string? Reason);

public sealed record ReasonRequest(string? Reason);

public sealed record RegisterDeviceRequest(string? Name, string? Type, string? StationCode, string? Location);

// The Roles and permissions page: every role, every permission, and which role holds which.
internal static class RoleMatrix
{
    private static readonly Dictionary<string, (string Label, string Group)> Labels = new()
    {
        [Permissions.JourneyRead] = ("View live departures and journeys", "Journeys"),
        [Permissions.OpsRead] = ("View the live operations board", "Journeys"),
        [Permissions.TicketPurchase] = ("Buy and hold tickets", "Tickets"),
        [Permissions.TicketRead] = ("Read own tickets", "Tickets"),
        [Permissions.TicketInspect] = ("Inspect tickets", "Tickets"),
        [Permissions.JourneyStopEventRecord] = ("Record stop events", "Journeys"),
        [Permissions.JourneyDelayReasonRecord] = ("Record delay reasons", "Journeys"),
        [Permissions.AssistanceRequest] = ("Request assistance", "Journeys"),
        [Permissions.JourneyCancel] = ("Cancel a journey", "Journeys"),
        [Permissions.JourneyReinstate] = ("Reinstate a cancelled journey", "Journeys"),
        [Permissions.JourneyTerminateShort] = ("Terminate a journey short", "Journeys"),
        [Permissions.JourneyInterrupt] = ("Interrupt and resume a journey", "Journeys"),
        [Permissions.JourneyTerminateEmergency] = ("Emergency termination", "Incidents"),
        [Permissions.JourneyPlatformChange] = ("Change a platform", "Journeys"),
        [Permissions.JourneyDataCorrect] = ("Correct journey data", "Journeys"),
        [Permissions.DisruptionCreate] = ("Create disruptions", "Disruptions"),
        [Permissions.DisruptionCreatePlanned] = ("Create planned works", "Disruptions"),
        [Permissions.DisruptionPublish] = ("Publish disruptions", "Disruptions"),
        [Permissions.DisruptionApproveNetworkWide] = ("Approve network-wide notices", "Disruptions"),
        [Permissions.DisruptionRetract] = ("Retract disruptions", "Disruptions"),
        [Permissions.IncidentDeclare] = ("Declare an incident", "Incidents"),
        [Permissions.IncidentDeclareCritical] = ("Declare a critical incident", "Incidents"),
        [Permissions.IncidentReassignOwner] = ("Hand over an incident", "Incidents"),
        [Permissions.MessageSendToDriver] = ("Message drivers", "Messages"),
        [Permissions.MessageAcknowledge] = ("Acknowledge messages", "Messages"),
        [Permissions.PlanningRead] = ("See planning and assignments", "Planning"),
        [Permissions.JourneyVehicleAssign] = ("Assign units", "Planning"),
        [Permissions.JourneyDriverAssign] = ("Assign drivers", "Planning"),
        [Permissions.VehicleAvailabilitySet] = ("Mark a unit out of service", "Planning"),
        [Permissions.TimetableRead] = ("Read timetables", "Timetables"),
        [Permissions.TimetableDraft] = ("Draft timetables", "Timetables"),
        [Permissions.TimetableApprove] = ("Approve timetables (not your own)", "Timetables"),
        [Permissions.StationInfoEdit] = ("Edit own station information", "Stations"),
        [Permissions.UserManage] = ("Manage users", "Administration"),
        [Permissions.RoleAssign] = ("Assign roles", "Administration"),
        [Permissions.SecuritySettingsManage] = ("Change security settings", "Administration"),
        [Permissions.DeviceProvision] = ("Register devices", "Devices"),
        [Permissions.DeviceRevoke] = ("Revoke devices", "Devices"),
        [Permissions.DeviceHealthRead] = ("See device health", "Devices"),
        [Permissions.AuditReadOperational] = ("Read the operational audit log", "Audit"),
        [Permissions.AuditReadAccess] = ("Read the access audit log", "Audit"),
        [Permissions.ReportRead] = ("Read reports", "Reports")
    };

    private static readonly Dictionary<string, string> RoleNames = new()
    {
        [Roles.Passenger] = "Passenger",
        [Roles.Driver] = "Driver",
        [Roles.Inspector] = "Ticket inspector",
        [Roles.Controller] = "Network controller",
        [Roles.StationMaster] = "Station master",
        [Roles.Scheduler] = "Scheduler",
        [Roles.Admin] = "System administrator"
    };

    public static object Build()
    {
        var roles = Roles.All.Select(r => new { code = r, name = RoleNames[r], permissions = RolePermissions.For(r).Order().ToArray() }).ToArray();
        var permissions = Permissions.All
            .Where(Labels.ContainsKey)
            .Select(p => new { key = p, label = Labels[p].Label, group = Labels[p].Group, roles = Roles.All.Where(r => RolePermissions.For(r).Contains(p)).ToArray() })
            .ToArray();

        return new
        {
            separationOfDuties = "No role can both run trains and manage access. Network-wide notices need a second controller.",
            note = "Read-only here; changes go through a reviewed release. Drivers inspect tickets only when given the inspector permission.",
            roles,
            permissions
        };
    }
}
