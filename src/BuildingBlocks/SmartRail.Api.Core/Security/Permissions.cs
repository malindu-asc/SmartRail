namespace SmartRail.Api.Core.Security;

// Every permission the API checks (permission-matrix.md). A permission is also the name of its authorisation policy, so an
// endpoint says what it needs: .RequirePermission(Permissions.JourneyCancel). Roles are bundles of permissions (RolePermissions).
public static class Permissions
{
    // Journeys and running
    public const string JourneyRead = "journey.read";

    // The live board and journey detail for staff: the whole network (a station master: their stations). JourneyRead alone is the
    // passenger and driver view (public journeys, own duty), so it never opens the console.
    public const string OpsRead = "ops.read";
    public const string JourneyCancel = "journey.cancel";
    public const string JourneyReinstate = "journey.reinstate";
    public const string JourneyTerminateShort = "journey.terminate_short";
    public const string JourneyInterrupt = "journey.interrupt";
    public const string JourneyTerminateEmergency = "journey.terminate_emergency";
    public const string JourneyPlatformChange = "journey.platform.change";
    public const string JourneyVehicleAssign = "journey.vehicle.assign";
    public const string JourneyDriverAssign = "journey.driver.assign";
    public const string JourneyStopEventRecord = "journey.stop_event.record";
    public const string JourneyDelayReasonRecord = "journey.delay_reason.record";
    public const string JourneyDataCorrect = "journey.data.correct";
    public const string AssistanceRequest = "assistance.request";

    // Planning, timetable, fleet
    public const string PlanningRead = "planning.read";
    public const string TimetableRead = "timetable.read";
    public const string TimetableDraft = "timetable.draft";
    public const string TimetableApprove = "timetable.approve";
    public const string VehicleAvailabilitySet = "vehicle.availability.set";
    public const string StationInfoEdit = "station.info.edit";

    // Disruptions, incidents, messages
    public const string DisruptionCreate = "disruption.create";
    public const string DisruptionCreatePlanned = "disruption.create_planned";
    public const string DisruptionPublish = "disruption.publish";
    public const string DisruptionApproveNetworkWide = "disruption.approve_network_wide";
    public const string DisruptionRetract = "disruption.retract";
    public const string IncidentDeclare = "incident.declare";
    public const string IncidentDeclareCritical = "incident.declare_critical";
    public const string IncidentReassignOwner = "incident.reassign_owner";
    public const string MessageSendToDriver = "message.send_to_driver";
    public const string MessageAcknowledge = "message.acknowledge";

    // Administration, devices, data
    public const string UserManage = "user.manage";
    public const string RoleAssign = "role.assign";
    public const string SecuritySettingsManage = "security.settings.manage";
    public const string DeviceProvision = "device.provision";
    public const string DeviceRevoke = "device.revoke";
    public const string DeviceHealthRead = "device.health.read";
    public const string AuditReadOperational = "audit.read.operational";
    public const string AuditReadAccess = "audit.read.access";
    public const string ReportRead = "report.read";

    // Passenger and inspector (tickets keep their own checks; listed so the matrix is complete)
    public const string TicketPurchase = "ticket.purchase";
    public const string TicketRead = "ticket.read";
    public const string TicketInspect = "ticket.inspect";

    public static readonly IReadOnlyList<string> All =
    [
        JourneyRead, OpsRead, JourneyCancel, JourneyReinstate, JourneyTerminateShort, JourneyInterrupt, JourneyTerminateEmergency,
        JourneyPlatformChange, JourneyVehicleAssign, JourneyDriverAssign, JourneyStopEventRecord, JourneyDelayReasonRecord,
        JourneyDataCorrect, AssistanceRequest, PlanningRead, TimetableRead, TimetableDraft, TimetableApprove,
        VehicleAvailabilitySet, StationInfoEdit, DisruptionCreate, DisruptionCreatePlanned, DisruptionPublish,
        DisruptionApproveNetworkWide, DisruptionRetract, IncidentDeclare, IncidentDeclareCritical, IncidentReassignOwner,
        MessageSendToDriver, MessageAcknowledge, UserManage, RoleAssign, SecuritySettingsManage, DeviceProvision, DeviceRevoke,
        DeviceHealthRead, AuditReadOperational, AuditReadAccess, ReportRead, TicketPurchase, TicketRead,
        TicketInspect
    ];
}

// Which permissions each role holds. The System Administrator holds no operational permission on purpose: one compromised
// administrator account must not be able to run the railway (separation of duties, REQ-OPS-001).
public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map = new Dictionary<string, IReadOnlyList<string>>
    {
        [Roles.Passenger] = [Permissions.JourneyRead, Permissions.TicketPurchase, Permissions.TicketRead],

        [Roles.Driver] =
        [
            Permissions.JourneyRead, Permissions.JourneyStopEventRecord, Permissions.JourneyDelayReasonRecord,
            Permissions.AssistanceRequest, Permissions.MessageAcknowledge
        ],

        [Roles.Inspector] = [Permissions.TicketInspect],

        // Network Controller, with the Incident Manager and Reporting Analyst bundles (ASM-047).
        [Roles.Controller] =
        [
            Permissions.JourneyRead, Permissions.OpsRead, Permissions.JourneyCancel, Permissions.JourneyReinstate, Permissions.JourneyTerminateShort,
            Permissions.JourneyInterrupt, Permissions.JourneyTerminateEmergency, Permissions.JourneyPlatformChange,
            Permissions.JourneyStopEventRecord, Permissions.JourneyDelayReasonRecord, Permissions.JourneyDataCorrect,
            Permissions.PlanningRead, Permissions.TimetableRead,
            Permissions.DisruptionCreate, Permissions.DisruptionPublish, Permissions.DisruptionApproveNetworkWide, Permissions.DisruptionRetract,
            Permissions.IncidentDeclare, Permissions.IncidentDeclareCritical, Permissions.IncidentReassignOwner,
            Permissions.MessageSendToDriver, Permissions.DeviceHealthRead,
            Permissions.AuditReadOperational, Permissions.AuditReadAccess, Permissions.ReportRead
        ],

        // Own station only (the scope is checked in the handler, not here).
        [Roles.StationMaster] = [Permissions.JourneyRead, Permissions.OpsRead, Permissions.StationInfoEdit, Permissions.DeviceHealthRead, Permissions.ReportRead],

        // Scheduler, with the Fleet Controller bundle.
        [Roles.Scheduler] =
        [
            Permissions.JourneyRead, Permissions.OpsRead, Permissions.PlanningRead, Permissions.TimetableRead, Permissions.TimetableDraft,
            Permissions.TimetableApprove, Permissions.JourneyVehicleAssign, Permissions.JourneyDriverAssign,
            Permissions.VehicleAvailabilitySet, Permissions.DisruptionCreatePlanned, Permissions.ReportRead
        ],

        [Roles.Admin] =
        [
            Permissions.UserManage, Permissions.RoleAssign, Permissions.SecuritySettingsManage, Permissions.DeviceProvision,
            Permissions.DeviceRevoke, Permissions.DeviceHealthRead, Permissions.AuditReadAccess
        ]
    };

    public static IReadOnlyList<string> For(string role) => Map.TryGetValue(role, out var p) ? p : [];

    public static IReadOnlySet<string> For(IEnumerable<string> roles) =>
        roles.SelectMany(For).ToHashSet(StringComparer.Ordinal);

    public static bool Allows(IEnumerable<string> roles, string permission) =>
        roles.Any(r => For(r).Contains(permission, StringComparer.Ordinal));

    // The matrix as data, for the read-only "Roles and permissions" page.
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Matrix => Map;
}
