// /api/ops/messages and /api/ops/assistance (guide, section 4.5)

export type MessageState = 'Sent' | 'Delivered' | 'Acknowledged'

export interface Message {
  id: string
  driverSubject: string
  driverName: string | null
  unitCode: string | null
  trainNumber: string | null
  journeyId: string | null
  template: string | null
  body: string
  requiresAck: boolean
  validUntilUtc: string
  sentAtUtc: string
  deliveredAtUtc: string | null
  acknowledgedAtUtc: string | null
  state: MessageState
  overdue: boolean
  overdueMinutes: number | null
  expired: boolean
  resentCount: number
  sentBy: string
}

export interface MessageList {
  open: number
  items: Message[]
}

export interface MessageTemplate {
  code: string
  label: string
  body: string
}

export interface Assistance {
  id: string
  driverSubject: string
  driverName: string | null
  trainNumber: string | null
  stationCode: string | null
  stationName: string | null
  note: string | null
  requestedAtUtc: string
  status: 'Open' | 'Handled'
  handledBy: string | null
  outcome: string | null
}

export interface SendMessageBody {
  driverSubject?: string
  journeyId?: string
  incidentId?: string
  template?: string
  body: string
  requiresAck: boolean
  validUntilUtc: string
}
