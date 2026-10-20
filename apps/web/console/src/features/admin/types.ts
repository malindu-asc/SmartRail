// /api/admin (guide, section 4.2)

export type StaffStatus = 'Active' | 'Locked' | 'Disabled' | 'Leaver'

export interface StaffAccount {
  id: string
  userName: string
  fullName: string
  email: string
  roles: string[]
  status: StaffStatus | string
  failedSignIns: number
  lastSignInUtc: string | null
  stationCodes: string[]
  employmentEnd: string | null
}

export interface StaffInvite {
  account: StaffAccount
  emailSent: boolean
  /** Only when no email service is configured (development). Shown once, never stored. */
  temporaryPassword: string | null
}

export interface UserFilters {
  role?: string
  status?: string
  station?: string
  q?: string
}

export interface RoleMatrix {
  separationOfDuties: string
  note: string
  roles: { code: string; name: string; permissions: string[] }[]
  permissions: { key: string; label: string; group: string; roles: string[] }[]
}

export type DeviceHealth = 'Online' | 'Late' | 'Offline' | 'Pending' | 'Revoked'

export interface Device {
  id: string
  name: string
  type: string
  stationCode: string
  stationName: string
  location: string | null
  health: DeviceHealth
  lastHeartbeatUtc: string | null
  lastHeartbeatAgeSeconds: number | null
  softwareVersion: string | null
  registeredBy: string
  registeredAt: string
  revokedAt: string | null
  revokedBy: string | null
  revokedReason: string | null
}

export interface DeviceList {
  items: Device[]
  counts: { all: number; displays: number; gates: number; terminals: number }
}

export interface DeviceFilters {
  type?: string
  health?: string
  station?: string
  q?: string
}

export interface SecuritySettings {
  lockAfterFailures: number
  lockMinutes: number
  twoStepRequired: boolean
  consoleIdleMinutes: number
  driverAppLockMinutes: number
  accessReviewDays: number
  flagLeavers: boolean
  deviceLateAfterSeconds: number
  deviceOfflineAfterSeconds: number
}

export interface SecurityView {
  settings: SecuritySettings
  lastAccessReview: string | null
  nextAccessReview: string | null
}

export interface AdminDashboard {
  staffAccounts: number
  rolesInUse: number
  staffByRole: Record<string, number>
  signedInNow: number
  devices: { total: number; online: number; needAttention: number }
  failedSignIns24h: number
  lockedAccounts: number
  accessReview: { daysLeft: number | null; nextReview: string | null; lastReview: string | null; intervalDays: number }
  needsAttention: { kind: string; title: string; detail: string; targetType: string; targetId: string; action: string }[]
  recentAccessChanges: { occurredAt: string; text: string; actorName: string | null }[]
}
