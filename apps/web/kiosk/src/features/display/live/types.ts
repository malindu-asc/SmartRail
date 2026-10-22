// What the public API answers. Nothing here is written by the display: it only reads.

export interface BoardStation {
  code: string
  name: string
}

export interface Board<T> {
  timeZone: string
  /** The server's clock when it answered. The display keeps time from this, never from its own clock. */
  asOfUtc: string
  station: BoardStation
  items: T[]
}

interface TrainItem {
  journeyId: string
  trainNumber: string
  routeCode: string
  estimateSource: 'Confirmed' | 'Live' | 'Scheduled'
  delayMinutes: number | null
  platform: string | null
  platformState: 'TBA' | 'Assigned' | 'Changed'
  /** Planned, Running, Cancelled, Completed, or Arrived on the arrivals board. */
  status: string
  confidence: 'Confirmed' | 'Live' | 'Scheduled' | 'Unknown'
  isStale: boolean
  ageSeconds: number | null
}

export interface DepartureItem extends TrainItem {
  destinationCode: string
  destinationName: string
  scheduledDepartureUtc: string
  expectedDepartureUtc: string
  /** True while the train has arrived at this station and not yet left. */
  atPlatform: boolean
}

export interface ArrivalItem extends TrainItem {
  originCode: string
  originName: string
  scheduledArrivalUtc: string
  expectedArrivalUtc: string
}

export interface Notice {
  id: string
  title: string
  message: string
  severity: 'Minor' | 'Major' | 'Severe'
  networkWide: boolean
  stations: { code: string; name: string }[]
  updatedAtUtc: string
  expectedEndUtc: string | null
  startsAtUtc: string | null
  /** True for works that start later: not a notice yet. */
  planned: boolean
}

export interface JourneyDetail {
  stops: { sequence: number; stationCode: string; stationName: string }[]
}
