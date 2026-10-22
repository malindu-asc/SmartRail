// Shapes returned by /api/ops/journeys and friends (docs/guides/ops-admin-console-api.md, section 4.3).

export type DataFreshness = 'Live' | 'Stale' | 'Unknown' | 'Scheduled' | 'Confirmed'

export interface JourneyRow {
  journeyId: string
  trainNumber: string
  routeCode: string
  originCode: string
  originName: string
  destinationCode: string
  destinationName: string
  scheduledDepartureUtc: string
  expectedDepartureUtc: string
  lifecycle: string
  statusCode: string
  statusLabel: string
  /** Good, Warning, Critical, Info, Stale or Neutral: the colour family of the chip. */
  statusKind: string
  nextStopCode: string | null
  nextStopName: string | null
  platform: string | null
  originalPlatform: string | null
  platformState: string
  delayMinutes: number | null
  punctuality: string | null
  data: DataFreshness
  dataAgeSeconds: number | null
  lastUpdateUtc: string | null
  driverSubject: string | null
  driverName: string | null
  unitId: string | null
  unitCode: string | null
  needsAttention: boolean
  attention: string[]
  /** Send back with every action so two people cannot act on one old view. */
  version: number
}

export interface Board {
  timeZone: string
  asOfUtc: string
  serviceDate: string
  total: number
  needAttention: number
  items: JourneyRow[]
}

export interface BoardFilters {
  date?: string
  route?: string
  station?: string
  status?: string
  attention?: boolean
  q?: string
}

export interface StopView {
  sequence: number
  stationCode: string
  stationName: string
  kind: string
  scheduledArrivalUtc: string | null
  scheduledDepartureUtc: string | null
  actualArrivalUtc: string | null
  actualDepartureUtc: string | null
  expectedArrivalUtc: string | null
  expectedDepartureUtc: string | null
  platform: string | null
  originalPlatform: string | null
  platformState: string
  state: string
  source: string
}

export interface Crew {
  driverSubject: string | null
  driverName: string | null
  dutyFromUtc: string | null
  dutyToUtc: string | null
  unitId: string | null
  unitCode: string | null
  unitName: string | null
  lastDriverEvent: string | null
}

/** What the journey's state allows; `permission` is what the person also needs. */
export interface JourneyAction {
  code: string
  allowed: boolean
  whyNot: string | null
  permission: string
}

export interface JourneyDetail {
  journey: JourneyRow
  stops: StopView[]
  crew: Crew
  actions: JourneyAction[]
  cancelReason: string | null
  endReason: string | null
  controllerNote: string | null
  endedAtUtc: string | null
}

export interface JourneyEvent {
  id: number
  occurredAtUtc: string
  receivedAtUtc: string
  stopSequence: number
  stationCode: string
  text: string
  source: string
  disposition: string
  note: string | null
}

export interface ReasonCode {
  code: string
  label: string
}

export interface ReasonCatalog {
  cancel: ReasonCode[]
  terminateShort: ReasonCode[]
  interrupt: ReasonCode[]
  platform: ReasonCode[]
}

export interface ActionResult {
  journeyId: string
  lifecycle: string
  version: number
}

export type ImpactAction = 'cancel' | 'terminate_short' | 'platform' | 'interrupt'

export interface Impact {
  journeyId: string
  trainNumber: string
  action: string
  tickets: number
  travellers: number
  /** Null until follow exists (SPEC-020): shown as "not available", never as 0. */
  followers: number | null
  stationDisplays: number
  driverName: string | null
  lines: string[]
}

export interface PlatformCheck {
  stationCode: string
  platform: string
  currentPlatform: string | null
  available: boolean
  conflicts: { journeyId: string; trainNumber: string; destinationStationCode: string; from: string; to: string }[]
}

export interface RouteSummary {
  key: string
  routeCode: string
  name: string
  status: string
  statusKind: string
  runningNow: number
  onTimeRunning: number
  dataUnknown: boolean
  unassigned: number
  note: string | null
  journeysToday: number
}

export interface RouteJourney {
  journeyId: string
  trainNumber: string
  departsUtc: string
  route: string
  driverSubject: string | null
  driverName: string | null
  unitCode: string | null
  assignment: string
  assignmentKind: string
}

export interface RouteDetail {
  summary: RouteSummary
  needAttention: number
  attentionText: string | null
  journeys: RouteJourney[]
}
