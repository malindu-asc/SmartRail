// /api/ops/disruptions and /api/ops/planned-works (guide, section 4.4)

export type DisruptionSeverity = 'Minor' | 'Major' | 'Severe'
export type DisruptionStatus = 'Draft' | 'AwaitingApproval' | 'Published' | 'Rejected' | 'Resolved' | 'Retracted'
export type DisruptionTab = 'active' | 'awaiting' | 'drafts' | 'resolved' | 'planned' | 'all'

export interface DisruptionRow {
  id: string
  title: string
  /** "All stations" or the station names */
  scope: string
  severity: DisruptionSeverity
  status: DisruptionStatus
  type: string | null
  publishedAtUtc: string | null
  reviewByUtc: string
  reviewExpired: boolean
  startsAtUtc: string | null
  planned: boolean
  ownerSubject: string
  ownerName: string | null
}

export interface DisruptionList {
  counts: { active: number; awaitingApproval: number; drafts: number; resolved: number; expired: number }
  items: DisruptionRow[]
}

export interface DisruptionPreview {
  app: { title: string; detail: string; severity: string }
  platformDisplay: string
  terminal: string
}

export interface DisruptionDetail {
  id: string
  title: string
  message: string
  severity: DisruptionSeverity
  status: DisruptionStatus
  type: string | null
  networkWide: boolean
  stations: { code: string; name: string }[]
  expectedEndUtc: string | null
  startsAtUtc: string | null
  reviewByUtc: string
  reviewExpired: boolean
  ownerSubject: string
  ownerName: string | null
  approvedBy: string | null
  rejectedBy: string | null
  rejectionComment: string | null
  journeysAffected: number | null
  preview: DisruptionPreview
  history: { at: string; text: string; person: string | null }[]
  /** True when this person may approve it now: a different controller, and it is waiting. */
  canApprove: boolean
}

export interface DisruptionBody {
  title: string
  message: string
  severity: DisruptionSeverity
  networkWide: boolean
  stationCodes: string[]
  type?: string
  startsAtUtc?: string
  expectedEndUtc?: string
  reviewByUtc: string
  submit: boolean
}

export const DISRUPTION_TYPES = ['Infrastructure fault', 'Train fault', 'Weather', 'Staffing', 'Facilities', 'Other'] as const
