// /api/ops/incidents (guide, section 4.5)

export type IncidentSeverity = 'Minor' | 'Major' | 'Critical'

export interface Incident {
  id: string
  title: string
  severity: IncidentSeverity
  status: 'Open' | 'Resolved'
  ownerSubject: string
  ownerName: string | null
  openedAtUtc: string
  stationCodes: string[]
}

export interface TimelineEntry {
  at: string
  kind: 'Note' | 'Action' | 'Handover' | 'System'
  text: string
  detail: string | null
  author: string
  authorName: string | null
}

export interface IncidentDetail {
  incident: Incident
  reportedBy: string | null
  resolvedAtUtc: string | null
  resolutionNote: string | null
  timeline: TimelineEntry[]
  told: { passengerAlerts: number | null; stationDisplays: number; driverMessages: number; driverMessagesAcknowledged: number }
  linked: { disruptionId: string | null; disruptionTitle: string | null; reviewByUtc: string | null; journeysAffected: number }
}

export interface SignOutCheck {
  ownedIncidents: Incident[]
  draftNotices: number
  mustHandOver: boolean
}

export interface DeclareBody {
  title: string
  severity: IncidentSeverity
  stationCodes: string[]
  reportedBy?: string
  disruptionId?: string
  confirm: boolean
  reason?: string
}
