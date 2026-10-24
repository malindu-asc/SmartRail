import { useEffect, useRef, useState } from 'react'
import { ApiError, getJson } from './api.ts'
import { linkState, type Link } from './logic.ts'
import type { ArrivalItem, Board, DepartureItem, Notice } from './types.ts'

/** How often a display asks again. Slower after failures, with a little jitter so a hundred displays do not all retry together (EDG-083). */
const POLL_MS = 10_000
const BACKOFF_MS = [10_000, 15_000, 20_000, 30_000]

interface Snapshot {
  stationCode: string
  stationName: string
  timeZone: string
  departures: DepartureItem[]
  arrivals: ArrivalItem[]
  notices: Notice[]
  /** When the display last got an answer, by the screen's own timer (only used to measure how long ago). */
  answeredAt: number
  /** The server's own time at that answer, which is what the footer shows as "updated". */
  answeredServerMs: number
  /** The server's clock minus the screen's at that moment, so a wrong device clock changes nothing (REQ-DSP-006). */
  offsetMs: number
}

export type StationState =
  | { kind: 'loading' }
  | { kind: 'unknown-station' }
  /** Never got an answer: the API could not be reached from the start. */
  | { kind: 'unavailable' }
  | { kind: 'ready'; data: Snapshot; link: Link; now: number }

/**
 * Keeps one station's board, arrivals and notices up to date from the public API, and says how fresh the information is. It keeps the
 * last answer when the API cannot be reached, and tells the screen how long it has been silent, so the screen can say so honestly.
 */
export function useStation(code: string): StationState {
  const [snapshot, setSnapshot] = useState<Snapshot | null>(null)
  const [state, setState] = useState<'loading' | 'unknown' | 'failing' | 'ok'>('loading')
  const [tick, setTick] = useState(() => Date.now())
  const failures = useRef(0)

  useEffect(() => {
    let cancelled = false
    let timer: ReturnType<typeof setTimeout> | undefined

    async function load() {
      const station = encodeURIComponent(code)
      const [departures, arrivals, notices] = await Promise.allSettled([
        getJson<Board<DepartureItem>>(`/api/journeys/boards/${station}?count=12`),
        getJson<Board<ArrivalItem>>(`/api/journeys/boards/${station}/arrivals?count=12`),
        getJson<Notice[]>('/api/operations/disruptions'),
      ])
      if (cancelled) return

      if (departures.status === 'rejected') {
        if (departures.reason instanceof ApiError && departures.reason.status === 404) {
          setState('unknown')
          return
        }
        failures.current += 1
        setState('failing')
      } else {
        failures.current = 0
        const board = departures.value
        const received = Date.now()
        setSnapshot((previous) => ({
          stationCode: board.station.code,
          stationName: board.station.name,
          timeZone: board.timeZone,
          departures: board.items,
          // The arrivals and the notices keep their last answer if only they failed.
          arrivals: arrivals.status === 'fulfilled' ? arrivals.value.items : (previous?.arrivals ?? []),
          notices: notices.status === 'fulfilled' ? notices.value : (previous?.notices ?? []),
          answeredAt: received,
          answeredServerMs: Date.parse(board.asOfUtc),
          offsetMs: Date.parse(board.asOfUtc) - received,
        }))
        setState('ok')
      }

      const wait = BACKOFF_MS[Math.min(failures.current, BACKOFF_MS.length - 1)] ?? POLL_MS
      timer = setTimeout(load, failures.current === 0 ? POLL_MS + Math.random() * 1000 : wait + Math.random() * 2000)
    }

    void load()
    return () => {
      cancelled = true
      clearTimeout(timer)
    }
  }, [code])

  // The clock the screen shows, and the age of the information, move every second.
  useEffect(() => {
    const id = setInterval(() => setTick(Date.now()), 1000)
    return () => clearInterval(id)
  }, [])

  if (state === 'unknown') return { kind: 'unknown-station' }
  if (!snapshot) return { kind: state === 'failing' ? 'unavailable' : 'loading' }

  return {
    kind: 'ready',
    data: snapshot,
    link: linkState(tick - snapshot.answeredAt),
    // Server time: the screen's clock plus the offset measured at the last answer.
    now: tick + snapshot.offsetMs,
  }
}
