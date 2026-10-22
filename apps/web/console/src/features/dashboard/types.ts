// GET /api/ops/dashboard (guide, section 4.3)

export interface Kpis {
  running: number
  onTimePercent: number | null
  delayed: number
  delayedOver15: number
  cancelled: number
  noLiveData: number
  disruptionsActive: number
  disruptionsAwaitingApproval: number
}

export type AttentionKind =
  | 'AssistanceRequested'
  | 'NoLiveData'
  | 'MessageNotAcknowledged'
  | 'NoDriver'
  | 'NoUnit'
  | 'UnitOutOfService'
  | 'MajorDelay'
  | 'Interrupted'
  | 'ApprovalNeeded'
  | 'DisruptionExpired'

export interface Attention {
  kind: AttentionKind | string
  title: string
  detail: string
  /** Open, Assign or Review */
  action: string
  /** journey, assistance, message or disruption */
  targetType: string
  targetId: string
  severity: number
}

export interface DeviceTypeCount {
  type: string
  total: number
  online: number
}

export interface DashboardDevices {
  total: number
  online: number
  late: number
  offline: number
  byType: DeviceTypeCount[]
}

export interface DashboardDisruption {
  id: string
  title: string
  severity: string
  sinceUtc: string | null
  scope: string
}

export interface Dashboard {
  asOfUtc: string
  kpis: Kpis
  needsAttention: Attention[]
  devices: DashboardDevices
  activeDisruptions: DashboardDisruption[]
}
