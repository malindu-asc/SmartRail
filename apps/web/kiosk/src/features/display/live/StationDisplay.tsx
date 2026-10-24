import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Fit } from '../Fit.tsx'
import { getJson } from './api.ts'
import { boardSeconds, noticesFor } from './logic.ts'
import { LiveBoard, LiveNotices, LivePlatform, StatusScreen } from './screens.tsx'
import { useStation } from './useStation.ts'

type Screen = 'platform' | 'departures' | 'arrivals' | 'notices'

/** How long each screen stays up, in seconds. A board with more than two pages of trains stays up long enough to show them all. */
const SECONDS = { platform: 10, notices: 10 }

/**
 * The display of one station, as a real wall screen: it fetches the live board, arrivals and notices, and moves between its screens by
 * itself. The notices screen only joins the cycle while the controller has published something for this station.
 * `/display/CEN` shows the whole station; `/display/CEN/1` is the display of platform 1.
 */
export function StationDisplay() {
  const { station = '', platform } = useParams()
  const state = useStation(station.toUpperCase())
  const [index, setIndex] = useState(0)

  const hasNotices = state.kind === 'ready' && noticesFor(state.data.notices, state.data.stationCode).length > 0
  const screens: Screen[] = ['platform', 'departures', 'arrivals', ...(hasNotices ? (['notices'] as const) : [])]
  const current = screens[index % screens.length] ?? 'platform'

  const departureRows = state.kind === 'ready' ? state.data.departures.length : 0
  const arrivalRows = state.kind === 'ready' ? state.data.arrivals.length : 0
  const seconds =
    current === 'departures' ? boardSeconds(departureRows) : current === 'arrivals' ? boardSeconds(arrivalRows) : SECONDS[current]

  useEffect(() => {
    const timer = setTimeout(() => setIndex((i) => i + 1), seconds * 1000)
    return () => clearTimeout(timer)
  }, [seconds, index])

  const stationName = state.kind === 'ready' ? state.data.stationName : null
  useEffect(() => {
    document.title = stationName ? `${stationName} · SmartRail display` : 'SmartRail display'
  }, [stationName])

  return (
    <div className="fullscreen">
      <Fit>
        {/* Its key changes with the screen, so each one fades up as it takes over. */}
        <div key={state.kind === 'ready' ? current : state.kind} className="d-enter">
          {state.kind !== 'ready' ? (
            <StatusScreen kind={state.kind} />
          ) : current === 'platform' ? (
            <LivePlatform ready={state} platform={platform ?? null} />
          ) : current === 'notices' ? (
            <LiveNotices ready={state} />
          ) : (
            <LiveBoard ready={state} kind={current} />
          )}
        </div>
      </Fit>
    </div>
  )
}

interface StationList {
  items: { code: string; name: string }[]
}

/** The start page: pick a station to open its display. Each display has its own address, which is what a terminal is set to open. */
export function StationPicker() {
  const [stations, setStations] = useState<StationList['items'] | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    getJson<StationList>('/api/network/stations')
      .then((list) => setStations(list.items))
      .catch(() => setFailed(true))
  }, [])

  return (
    <div className="picker">
      <h1>Station displays</h1>
      <p style={{ color: 'var(--text-dim)' }}>Open the display of a station. Each one reads the live board from the system.</p>
      {failed && <p>The station list could not be loaded. Check that the system is running.</p>}
      {!stations && !failed && <p>Loading…</p>}
      {stations?.map((s) => (
        <Link key={s.code} to={`/display/${s.code}`}>
          {s.name} <small>/display/{s.code}</small>
        </Link>
      ))}
      <p style={{ marginTop: 32 }}>
        <Link to="/gallery">Design review with sample data</Link>
      </p>
    </div>
  )
}
