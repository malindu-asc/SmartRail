import { Permissions as P } from '@/shared/auth'

/**
 * The permissions each role holds, copied from src/BuildingBlocks/SmartRail.Api.Core/Security/Permissions.cs (RolePermissions).
 * Tests use this to act as each role. If the API's matrix changes, this and the tests change with it.
 */
export const RolePermissionsForTests = {
  controller: [
    P.JourneyRead, P.OpsRead, P.JourneyCancel, P.JourneyReinstate, P.JourneyTerminateShort, P.JourneyInterrupt, P.JourneyTerminateEmergency,
    P.JourneyPlatformChange, P.PlanningRead, P.TimetableRead, P.DisruptionCreate, P.DisruptionPublish, P.DisruptionApproveNetworkWide,
    P.DisruptionRetract, P.IncidentDeclare, P.IncidentDeclareCritical, P.IncidentReassignOwner, P.MessageSendToDriver, P.DeviceHealthRead,
    P.AuditReadOperational, P.AuditReadAccess, P.ReportRead,
  ],
  scheduler: [
    P.JourneyRead, P.OpsRead, P.PlanningRead, P.TimetableRead, P.TimetableDraft, P.TimetableApprove, P.JourneyVehicleAssign,
    P.JourneyDriverAssign, P.VehicleAvailabilitySet, P.DisruptionCreatePlanned, P.ReportRead,
  ],
  station_master: [P.JourneyRead, P.OpsRead, P.DeviceHealthRead, P.ReportRead],
  admin: [P.UserManage, P.RoleAssign, P.SecuritySettingsManage, P.DeviceProvision, P.DeviceRevoke, P.DeviceHealthRead, P.AuditReadAccess],
  driver: [P.JourneyRead],
  passenger: [P.JourneyRead],
} as const
