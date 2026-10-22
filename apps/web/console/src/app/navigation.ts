import {
  Activity,
  AlertTriangle,
  CalendarDays,
  ClipboardList,
  FileClock,
  LayoutDashboard,
  MessageSquare,
  Monitor,
  Route,
  ShieldCheck,
  Siren,
  Users,
  type LucideIcon,
} from 'lucide-react'
import { hasAny, Permissions } from '@/shared/auth/permissions'

export interface NavItem {
  to: string
  label: string
  icon: LucideIcon
  /** Any one of these opens the item. The sidebar and the route table both read this, so a hidden item is also a blocked route. */
  permission: readonly string[]
  group: 'ops' | 'admin'
}

export const NAV_ITEMS: readonly NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, permission: [Permissions.PlanningRead], group: 'ops' },
  { to: '/live-board', label: 'Live board', icon: Activity, permission: [Permissions.OpsRead], group: 'ops' },
  { to: '/routes', label: 'Routes', icon: Route, permission: [Permissions.OpsRead], group: 'ops' },
  { to: '/disruptions', label: 'Disruptions', icon: AlertTriangle, permission: [Permissions.DisruptionCreate, Permissions.DisruptionCreatePlanned], group: 'ops' },
  { to: '/driver-messages', label: 'Driver messages', icon: MessageSquare, permission: [Permissions.MessageSendToDriver], group: 'ops' },
  { to: '/planning', label: 'Planning', icon: CalendarDays, permission: [Permissions.PlanningRead], group: 'ops' },
  { to: '/incidents', label: 'Incidents', icon: Siren, permission: [Permissions.IncidentDeclare], group: 'ops' },
  { to: '/timetables', label: 'Timetables', icon: ClipboardList, permission: [Permissions.TimetableRead], group: 'ops' },

  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard, permission: [Permissions.UserManage], group: 'admin' },
  { to: '/admin/users', label: 'Users', icon: Users, permission: [Permissions.UserManage], group: 'admin' },
  { to: '/admin/roles', label: 'Roles and permissions', icon: ShieldCheck, permission: [Permissions.UserManage], group: 'admin' },
  { to: '/admin/devices', label: 'Devices', icon: Monitor, permission: [Permissions.DeviceHealthRead], group: 'admin' },
  { to: '/admin/audit', label: 'Audit log', icon: FileClock, permission: [Permissions.AuditReadAccess], group: 'admin' },
]

/** The items a person may see. An administrator and a controller see different sets because their permissions differ. */
export function visibleNav(permissions: ReadonlySet<string>): NavItem[] {
  return NAV_ITEMS.filter((item) => hasAny(permissions, item.permission))
}

/**
 * The admin console has its own dark chrome and shows "Cannot operate trains". A person is in it when they manage users and
 * hold nothing operational: the administrator holds no operational permission on purpose.
 */
export function isAdminConsole(permissions: ReadonlySet<string>): boolean {
  return permissions.has(Permissions.UserManage) && !permissions.has(Permissions.OpsRead) && !permissions.has(Permissions.PlanningRead)
}

/** The items the sidebar shows: the admin group in the admin console, the operations group everywhere else. */
export function sidebarItems(permissions: ReadonlySet<string>): NavItem[] {
  const group = isAdminConsole(permissions) ? 'admin' : 'ops'
  return visibleNav(permissions).filter((item) => item.group === group)
}
