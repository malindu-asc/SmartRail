// Mirrors src/BuildingBlocks/SmartRail.Api.Core/Security/Permissions.cs. The API is the authority: every endpoint checks its permission.
// The console reads the same names from GET /api/me so it can hide what a person cannot do.

export const Permissions = {
  JourneyRead: 'journey.read',
  OpsRead: 'ops.read',
  JourneyCancel: 'journey.cancel',
  JourneyReinstate: 'journey.reinstate',
  JourneyTerminateShort: 'journey.terminate_short',
  JourneyInterrupt: 'journey.interrupt',
  JourneyTerminateEmergency: 'journey.terminate_emergency',
  JourneyPlatformChange: 'journey.platform.change',
  JourneyVehicleAssign: 'journey.vehicle.assign',
  JourneyDriverAssign: 'journey.driver.assign',
  PlanningRead: 'planning.read',
  TimetableRead: 'timetable.read',
  TimetableDraft: 'timetable.draft',
  TimetableApprove: 'timetable.approve',
  VehicleAvailabilitySet: 'vehicle.availability.set',
  DisruptionCreate: 'disruption.create',
  DisruptionCreatePlanned: 'disruption.create_planned',
  DisruptionPublish: 'disruption.publish',
  DisruptionApproveNetworkWide: 'disruption.approve_network_wide',
  DisruptionRetract: 'disruption.retract',
  IncidentDeclare: 'incident.declare',
  IncidentDeclareCritical: 'incident.declare_critical',
  IncidentReassignOwner: 'incident.reassign_owner',
  MessageSendToDriver: 'message.send_to_driver',
  UserManage: 'user.manage',
  RoleAssign: 'role.assign',
  SecuritySettingsManage: 'security.settings.manage',
  DeviceProvision: 'device.provision',
  DeviceRevoke: 'device.revoke',
  DeviceHealthRead: 'device.health.read',
  AuditReadOperational: 'audit.read.operational',
  AuditReadAccess: 'audit.read.access',
  ReportRead: 'report.read',
} as const

export type Permission = (typeof Permissions)[keyof typeof Permissions]

export const Roles = {
  Controller: 'controller',
  Scheduler: 'scheduler',
  StationMaster: 'station_master',
  Admin: 'admin',
  Driver: 'driver',
  Inspector: 'inspector',
  Passenger: 'passenger',
} as const

const ROLE_LABELS: Record<string, string> = {
  controller: 'Network controller',
  scheduler: 'Scheduler',
  station_master: 'Station master',
  admin: 'System administrator',
  driver: 'Driver',
  inspector: 'Ticket inspector',
  passenger: 'Passenger',
}

export function roleLabel(role: string): string {
  return ROLE_LABELS[role] ?? role
}

/** The label of the most senior console role a person holds. */
export function primaryRoleLabel(roles: readonly string[]): string {
  const order = [Roles.Admin, Roles.Controller, Roles.Scheduler, Roles.StationMaster, Roles.Driver, Roles.Inspector, Roles.Passenger]
  const found = order.find((role) => roles.includes(role))
  return found ? roleLabel(found) : 'Staff'
}

/** Which console a person lands in. Administrators run access, everyone else with operational rights runs the day. */
export function homePath(roles: readonly string[], permissions: ReadonlySet<string>): string {
  if (roles.includes(Roles.Admin) && permissions.has(Permissions.UserManage)) return '/admin'
  if (permissions.has(Permissions.PlanningRead)) return '/dashboard'
  if (permissions.has(Permissions.OpsRead)) return '/live-board'
  return '/no-access'
}

export function hasAny(permissions: ReadonlySet<string>, required: readonly string[]): boolean {
  return required.length === 0 || required.some((permission) => permissions.has(permission))
}
