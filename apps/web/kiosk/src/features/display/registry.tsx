import { Arrivals, Departures, PlatformDisrupted, PlatformNext } from './screens'

/**
 * The screens a display shows, in the order it cycles through them. `seconds` is how long each stays up. The no-live-data, closed and
 * pairing states are decided by the backend and the device, so they are not part of this review.
 */
export const screens = [
  { id: 'platform-next', group: 'Platform', title: 'Next train, on time', node: <PlatformNext />, board: 'Design 5', seconds: 10 },
  { id: 'platform-disrupted', group: 'Platform', title: 'Cancelled and platform changed', node: <PlatformDisrupted />, board: 'Design 6', seconds: 10 },
  { id: 'departures', group: 'Concourse', title: 'Departures board', node: <Departures />, board: 'Design 8', seconds: 14 },
  { id: 'arrivals', group: 'Concourse', title: 'Arrivals board', node: <Arrivals />, board: 'New', seconds: 14 },
] as const
