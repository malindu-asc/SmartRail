using SmartRail.Api.Core.Security;

namespace SmartRail.Modules.Operations.Tests;

// permission-matrix.md as tests: who holds what, and separation of duties (REQ-OPS-001).
public sealed class PermissionMatrixTests
{
    private static readonly string[] Operational =
    [
        Permissions.JourneyCancel, Permissions.JourneyReinstate, Permissions.JourneyTerminateShort, Permissions.JourneyInterrupt,
        Permissions.JourneyTerminateEmergency, Permissions.JourneyPlatformChange, Permissions.JourneyVehicleAssign, Permissions.JourneyDriverAssign,
        Permissions.TimetableDraft, Permissions.TimetableApprove, Permissions.VehicleAvailabilitySet, Permissions.DisruptionCreate,
        Permissions.DisruptionPublish, Permissions.DisruptionApproveNetworkWide, Permissions.DisruptionRetract, Permissions.IncidentDeclare,
        Permissions.IncidentDeclareCritical, Permissions.MessageSendToDriver
    ];

    [Fact]
    public void The_administrator_holds_no_operational_permission()
    {
        var admin = RolePermissions.For(Roles.Admin);
        Assert.Empty(admin.Intersect(Operational));
    }

    [Fact]
    public void Only_the_administrator_manages_users_roles_security_and_devices()
    {
        foreach (var permission in new[] { Permissions.UserManage, Permissions.RoleAssign, Permissions.SecuritySettingsManage, Permissions.DeviceProvision, Permissions.DeviceRevoke })
        {
            Assert.True(RolePermissions.Allows([Roles.Admin], permission));
            foreach (var role in new[] { Roles.Controller, Roles.Scheduler, Roles.Driver, Roles.Passenger, Roles.StationMaster, Roles.Inspector })
                Assert.False(RolePermissions.Allows([role], permission), $"{role} must not hold {permission}");
        }
    }

    [Fact]
    public void The_controller_runs_the_day_but_does_not_plan_the_timetable_or_assign_crew()
    {
        Assert.True(RolePermissions.Allows([Roles.Controller], Permissions.JourneyCancel));
        Assert.True(RolePermissions.Allows([Roles.Controller], Permissions.DisruptionApproveNetworkWide));
        Assert.False(RolePermissions.Allows([Roles.Controller], Permissions.TimetableDraft));
        Assert.False(RolePermissions.Allows([Roles.Controller], Permissions.JourneyVehicleAssign));
        Assert.False(RolePermissions.Allows([Roles.Controller], Permissions.UserManage));
    }

    [Fact]
    public void The_scheduler_plans_but_does_not_cancel_or_publish_notices()
    {
        Assert.True(RolePermissions.Allows([Roles.Scheduler], Permissions.TimetableDraft));
        Assert.True(RolePermissions.Allows([Roles.Scheduler], Permissions.JourneyVehicleAssign));
        Assert.True(RolePermissions.Allows([Roles.Scheduler], Permissions.DisruptionCreatePlanned));
        Assert.False(RolePermissions.Allows([Roles.Scheduler], Permissions.JourneyCancel));
        Assert.False(RolePermissions.Allows([Roles.Scheduler], Permissions.DisruptionPublish));
    }

    [Fact]
    public void The_driver_can_only_acknowledge_ask_for_help_and_record_events()
    {
        var driver = RolePermissions.For(Roles.Driver);
        Assert.Contains(Permissions.MessageAcknowledge, driver);
        Assert.Contains(Permissions.AssistanceRequest, driver);
        Assert.Empty(driver.Intersect(Operational));
    }

    [Fact]
    public void The_live_operations_board_is_for_staff_not_for_passengers_drivers_or_the_administrator()
    {
        foreach (var role in new[] { Roles.Controller, Roles.Scheduler, Roles.StationMaster })
            Assert.True(RolePermissions.Allows([role], Permissions.OpsRead));
        foreach (var role in new[] { Roles.Passenger, Roles.Driver, Roles.Inspector, Roles.Admin })
            Assert.False(RolePermissions.Allows([role], Permissions.OpsRead), $"{role} must not read the operations board");
    }

    [Fact]
    public void Only_the_administrator_and_the_controller_read_the_access_audit_log()
    {
        Assert.True(RolePermissions.Allows([Roles.Admin], Permissions.AuditReadAccess));
        Assert.True(RolePermissions.Allows([Roles.Controller], Permissions.AuditReadAccess));
        Assert.False(RolePermissions.Allows([Roles.Admin], Permissions.AuditReadOperational));
        Assert.False(RolePermissions.Allows([Roles.Scheduler], Permissions.AuditReadAccess));
    }

    [Fact]
    public void Every_role_permission_is_a_known_permission()
    {
        foreach (var (role, permissions) in RolePermissions.Matrix)
            foreach (var permission in permissions)
                Assert.Contains(permission, Permissions.All);
    }
}
