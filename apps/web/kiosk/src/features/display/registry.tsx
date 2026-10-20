import { Concourse, PlatformDisrupted, PlatformNext } from './screens'

/**
 * The screens a display shows, in the order it cycles through them. `seconds` is how long each stays up. The no-live-data, closed and
 * pairing states are decided by the backend and the device, so they are not part of this review.
 */
export const screens = [
  { id: 'platform-next', group: 'Platform', title: 'Next train, on time', node: <PlatformNext />, board: 'Design 5', seconds: 9 },
  { id: 'platform-disrupted', group: 'Platform', title: 'Cancelled and platform changed', node: <PlatformDisrupted />, board: 'Design 6', seconds: 9 },
  { id: 'concourse', group: 'Concourse', title: 'Departures board', node: <Concourse />, board: 'Design 8', seconds: 16 },
] as const
