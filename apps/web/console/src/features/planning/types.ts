// /api/ops/planning, /units, /crew, /duties and the assignment calls (guide, section 4.6)

export interface PlanningCheck {
  code: string
  label: string
  passed: boolean
}

export interface DriverOption {
  dutyId: string
  driverSubject: string
  driverName: string
  dutyFromUtc: string
  dutyToUtc: string
  eligible: boolean
  note: string
  checks: PlanningCheck[]
}

export interface UnitOption {
  unitId: string
  code: string
  name: string
  available: boolean
  note: string
}

export interface AssignmentOptions {
  journeyId: string
  trainNumber: string
  route: string
  departsUtc: string
  version: number
  drivers: DriverOption[]
  units: UnitOption[]
}

export interface PlanningNeed {
  journeyId: string
  trainNumber: string
  route: string
  departsUtc: string
  departsInMinutes: number
  /** Driver, Unit or both */
  missing: string
  detail: string
  suggestedDriver: DriverOption | null
  suggestedUnit: UnitOption | null
}

export interface UnitAlert {
  unitId: string
  code: string
  from: string
  to: string | null
  reason: string
  affectedJourneys: number
}

export interface PlanningNeeds {
  serviceDate: string
  unitAlerts: UnitAlert[]
  items: PlanningNeed[]
}

export interface UnitAssignmentRow {
  unitId: string
  code: string
  name: string
  drivers: string[]
  journeyTimes: string[]
  status: string
}

export interface UnitRow {
  unitId: string
  code: string
  name: string
  kind: string
  status: 'Available' | 'OutOfService' | string
  outOfServiceFromUtc: string | null
  outOfServiceToUtc: string | null
  reason: string | null
  journeysToday: number
}

export interface CrewRow {
  dutyId: string
  driverSubject: string
  driverName: string
  fromUtc: string
  toUtc: string
  state: string
  journeyTimes: string[]
}

export interface AffectedJourney {
  journeyId: string
  trainNumber: string
  route: string
  departsUtc: string
  currentUnit: string | null
  suggestedUnit: UnitOption | null
  status: string
}

export interface UnitImpact {
  unitId: string
  code: string
  periodId: string | null
  outOfServiceFromUtc: string | null
  reason: string | null
  uncovered: number
  journeys: AffectedJourney[]
}

export interface SuggestionOutcome {
  journeyId: string
  trainNumber: string
  applied: boolean
  error: string | null
}

export interface WeekJourney {
  journeyId: string
  trainNumber: string
  departsUtc: string
  route: string
  driverName: string | null
  unitCode: string | null
  assignment: string
  assignmentKind: string
}

export interface PlanningDay {
  date: string
  journeys: number
  unassigned: number
  approved: boolean
  rows: WeekJourney[]
}

export interface PlanningWeek {
  from: string
  days: PlanningDay[]
}
