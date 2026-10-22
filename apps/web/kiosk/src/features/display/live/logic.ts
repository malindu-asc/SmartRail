import type { Status } from '../parts.tsx'
import type { ArrivalItem, DepartureItem, Notice } from './types.ts'

/**
 * The rules of the display, as plain functions so they can be tested without a browser. The display never works out railway facts: the
 * API says how late a train is and which platform it uses. These rules only choose the words and the order to show them in.
 */

/** How the display's own connection to the API is doing (REQ-DSP-004). */
export type Link = 'live' | 'delayed' | 'offline'

/** After 60 seconds without an answer the board says live information is unavailable; after 300 it shows the timetable only. */
export const DELAYED_AFTER_MS = 60_000
export const OFFLINE_AFTER_MS = 300_000

export function linkState(sinceLastAnswerMs: number): Link {
  if (sinceLastAnswerMs >= OFFLINE_AFTER_MS) return 'offline'
  if (sinceLastAnswerMs >= DELAYED_AFTER_MS) return 'delayed'
  return 'live'
}

/** `HH:mm` in the network's time zone, never the screen's own. */
export function clock(instant: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone }).format(new Date(instant))
}

/** Whole minutes until a time, rounded up, so "1 min" shows until the minute is really over. */
export function minutesUntil(targetMs: number, nowMs: number): number {
  return Math.ceil((targetMs - nowMs) / 60_000)
}

/** What a train is doing, as a code the screen turns into words in the language it is showing. */
export type Code = 'onTime' | 'expected' | 'cancelled' | 'platformChanged' | 'scheduled' | 'departed' | 'arrived' | 'atPlatform'

export interface Row {
  key: string
  /** The scheduled time. The expected time, when different, is in the label. */
  time: string
  name: string
  platform: string
  status: Status
  /** The words in English. The screen uses `code` and `at` to say the same in another language. */
  label: string
  code: Code
  /** The expected time, when the code is `expected`. */
  at?: string
  /** The train is on a later day than today, so the time is marked "Tomorrow". */
  tomorrow: boolean
}

interface Context {
  link: Link
  timeZone: string
  /** The server's time now, to know which day a train is on. */
  nowMs: number
}

interface Common {
  atPlatform?: boolean
  journeyId: string
  status: string
  estimateSource: string
  confidence: string
  isStale: boolean
  delayMinutes: number | null
  platformState: string
}

/** Words and colour for a train, in one place for both boards. A cancelled train is always shown as cancelled, even without live data. */
function describe(item: Common, expected: string, link: Link, timeZone: string): { status: Status; label: string; code: Code; at?: string } {
  if (item.status === 'Cancelled') return { status: 'cancelled', label: 'Cancelled', code: 'cancelled' }

  // No live data at all: the display says so and shows the timetable, never a made-up estimate.
  const silent = item.isStale || (item.status === 'Running' && (item.confidence === 'Scheduled' || item.confidence === 'Unknown'))
  if (link === 'offline' || silent) return { status: 'scheduled', label: 'Scheduled', code: 'scheduled' }

  if (item.status === 'Arrived') return { status: 'on_time', label: 'Arrived', code: 'arrived' }
  // The train is here: that matters more than how late it was getting in.
  if (item.atPlatform) return { status: 'on_time', label: 'At platform', code: 'atPlatform' }
  if (item.estimateSource === 'Confirmed' && item.status !== 'Planned') return { status: 'on_time', label: 'Departed', code: 'departed' }
  if (item.platformState === 'Changed') return { status: 'changed', label: 'Platform changed', code: 'platformChanged' }
  if ((item.delayMinutes ?? 0) >= 1) {
    const at = clock(expected, timeZone)
    return { status: 'delayed', label: `Expected ${at}`, code: 'expected', at }
  }
  return { status: 'on_time', label: 'On time', code: 'onTime' }
}

/** The calendar day of an instant in the network's time zone, as `yyyy-mm-dd`. */
export function dayOf(instant: string | number | Date, timeZone: string): string {
  return new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone }).format(new Date(instant))
}

export function departureRow(item: DepartureItem, { link, timeZone, nowMs }: Context): Row {
  return {
    key: item.journeyId,
    tomorrow: dayOf(item.scheduledDepartureUtc, timeZone) > dayOf(nowMs, timeZone),
    time: clock(item.scheduledDepartureUtc, timeZone),
    name: item.destinationName,
    platform: item.platform ?? '–',
    ...describe(item, item.expectedDepartureUtc, link, timeZone),
  }
}

export function arrivalRow(item: ArrivalItem, { link, timeZone, nowMs }: Context): Row {
  return {
    key: item.journeyId,
    tomorrow: dayOf(item.scheduledArrivalUtc, timeZone) > dayOf(nowMs, timeZone),
    time: clock(item.scheduledArrivalUtc, timeZone),
    name: item.originName,
    platform: item.platform ?? '–',
    ...describe(item, item.expectedArrivalUtc, link, timeZone),
  }
}

const severityRank: Record<Notice['severity'], number> = { Severe: 3, Major: 2, Minor: 1 }

/** The notices that apply to a station: published (not works that start later), for this station or the whole network, worst first. */
export function noticesFor(notices: Notice[], stationCode: string): Notice[] {
  return notices
    .filter((n) => !n.planned && (n.networkWide || n.stations.some((s) => s.code === stationCode)))
    .sort((a, b) => severityRank[b.severity] - severityRank[a.severity] || b.updatedAtUtc.localeCompare(a.updatedAtUtc))
}

export interface PlatformModel {
  /** The train that matters now: the next one that has not left. */
  next: DepartureItem | null
  /** The two after it. */
  after: DepartureItem[]
  /** When the next train is cancelled: the next one to the same place that is running, so passengers have somewhere to go. */
  alternative: DepartureItem | null
}

/** What a platform display shows: the next train large, two more small. With no platform given, it is every train of the station. */
export function platformModel(departures: DepartureItem[], platform: string | null): PlatformModel {
  const here = departures.filter(
    (d) => (platform === null || d.platform === platform) && !(d.estimateSource === 'Confirmed' && d.status !== 'Planned' && d.status !== 'Cancelled'),
  )
  const [next = null, ...rest] = here
  const alternative =
    next?.status === 'Cancelled' ? (rest.find((d) => d.status !== 'Cancelled' && d.destinationCode === next.destinationCode) ?? null) : null
  return { next, after: rest.slice(0, 2), alternative }
}

/** The rows of a board a screen can hold at once. A longer board is shown a page at a time. */
export const PAGE_SIZE = 6
export const PAGE_SECONDS = 7

export function pagesOf<T>(items: T[], size: number = PAGE_SIZE): T[][] {
  const pages: T[][] = []
  for (let start = 0; start < items.length; start += size) pages.push(items.slice(start, start + size))
  return pages
}

/** How long a board stays up: 14 seconds, and 7 for each page when there are more than two. */
export function boardSeconds(rows: number): number {
  return Math.max(14, pagesOf(new Array(rows).fill(0)).length * PAGE_SECONDS)
}

/** How long a train that has gone takes to fade away before the rows close up. */
export const LEAVE_MS = 700

/** What changed between the rows on screen and the rows now: the trains that have gone, and the ones that are new. */
export function diffRows(onScreen: Row[], now: Row[]): { removed: string[]; added: string[] } {
  const before = new Set(onScreen.map((r) => r.key))
  const after = new Set(now.map((r) => r.key))
  return {
    removed: onScreen.filter((r) => !after.has(r.key)).map((r) => r.key),
    added: now.filter((r) => !before.has(r.key)).map((r) => r.key),
  }
}
