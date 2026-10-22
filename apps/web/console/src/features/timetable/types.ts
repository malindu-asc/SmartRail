// /api/ops/timetable/versions (guide, section 4.6)

export type VersionStatus = 'Draft' | 'InReview' | 'Published' | 'Superseded'

export interface Version {
  id: string
  label: string
  status: VersionStatus
  effectiveFrom: string | null
  author: string
  authorName: string | null
  approvedBy: string | null
  approvedByName: string | null
  services: number
  createdAtUtc: string
  publishedAtUtc: string | null
  inForce: boolean
}

export interface VersionStop {
  sequence: number
  stationCode: string
  stationName: string
  arrival: string | null
  departure: string | null
  arrivalMinutes: number | null
  departureMinutes: number | null
  kind: string
}

export interface VersionService {
  id: string
  trainNumber: string
  routeCode: string
  direction: string
  category: string
  calendarCode: string
  origin: string
  destination: string
  firstDeparture: string | null
  lastArrival: string | null
  stops: VersionStop[]
}

export interface VersionDetail {
  version: Version
  services: VersionService[]
  actions: { canEdit: boolean; canSubmit: boolean; canApprove: boolean; approveBlockedReason: string | null }
}

export interface Comparison {
  versionId: string
  againstId: string
  againstLabel: string
  added: number
  removed: number
  changed: number
  unchanged: number
  changes: { trainNumber: string; kind: 'Added' | 'Removed' | 'Changed'; summary: string }[]
}

export interface StopTimeInput {
  stationCode: string
  arrival: string | null
  departure: string | null
}

export interface AddServiceBody {
  trainNumber: string
  routeCode: string
  calendarCode: string
  direction: 'Forward' | 'Reverse'
  category: 'Regional' | 'Intercity' | 'HighSpeed'
  stops: StopTimeInput[]
}
