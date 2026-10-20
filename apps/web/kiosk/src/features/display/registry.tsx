import {
  Concourse,
  Pairing,
  PlatformDisrupted,
  PlatformEndOfService,
  PlatformNext,
  PlatformNoLive,
  StationClosed,
} from './screens'

/** Every display state, in the order the review page lists them. */
export const screens = [
  { id: 'platform-next', group: 'Platform', title: 'Next train, on time', node: <PlatformNext />, board: 'Design 5' },
  { id: 'platform-disrupted', group: 'Platform', title: 'Cancelled and platform changed', node: <PlatformDisrupted />, board: 'Design 6' },
  { id: 'platform-no-live', group: 'Platform', title: 'No live information', node: <PlatformNoLive />, board: 'Design 7' },
  { id: 'platform-end', group: 'Platform', title: 'No more trains today', node: <PlatformEndOfService />, board: 'New' },
  { id: 'concourse', group: 'Concourse', title: 'Departures board', node: <Concourse />, board: 'Design 8' },
  { id: 'concourse-no-live', group: 'Concourse', title: 'Timetable only', node: <Concourse stale />, board: 'New' },
  { id: 'closed', group: 'Concourse', title: 'Station closed', node: <StationClosed />, board: 'New' },
  { id: 'pairing', group: 'Device', title: 'Waiting to be paired', node: <Pairing />, board: 'New' },
] as const
