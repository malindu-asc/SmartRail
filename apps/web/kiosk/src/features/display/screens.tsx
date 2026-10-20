import { useEffect, useRef, useState } from 'react'
import { at, sampleMinutes, useClock } from './clock'
import { Icon } from './icons'
import { Banner, Footer, Header, Stage, StatusBadge, type Status } from './parts'

/**
 * The station displays at high fidelity. Nothing here is wired to the API yet: the numbers are sample data so the design can be
 * judged. A real display reads the next departures for its platform or station and uses server time, never the device clock.
 *
 * A whole screen fades up once, together (see Gallery), as a real display does when it changes.
 */

type Row = { time: string; to: string; status: Status; label: string }

function MiniRow({ row }: { row: Row }) {
  return (
    <div className="d-mini">
      <span className="d-mini-time">{row.time}</span>
      <span className="d-mini-to">{row.to}</span>
      <StatusBadge status={row.status} label={row.label} />
    </div>
  )
}

/** Platform display, a train on time: the next train large, the two after it small. Design 5. */
export function PlatformNext() {
  const { minute, label } = useClock()
  // The countdown follows the clock, so it falls by itself; at zero the train is at the platform.
  const left = Math.max(0, at(14, 25) - minute)

  return (
    <Stage>
      <Header title="Platform 2" time={label} />
      <section className="d-hero">
        <div className="d-hero-main">
          <div className="d-hero-line">
            <span className="d-hero-time">14:25</span>
            <span className="d-hero-to">Riverside</span>
          </div>
          <p className="d-calling">Calling at Oak Park, Mill Lane, Riverside</p>
          <div className="d-hero-meta">
            <StatusBadge status="on_time" label="On time" size="lg" />
            <span className="d-dim">4 cars</span>
          </div>
        </div>
        <div className="d-countdown">
          {left > 0 ? (
            <>
              <strong>{left}</strong>
              <span>min</span>
            </>
          ) : (
            <strong className="d-countdown-now">Now</strong>
          )}
        </div>
      </section>
      <MiniRow row={{ time: '14:52', to: 'Riverside', status: 'on_time', label: 'On time' }} />
      <MiniRow row={{ time: '15:20', to: 'Riverside', status: 'on_time', label: 'On time' }} />
      <Footer tone="live" icon="check-circle">
        Live · updated {label}
      </Footer>
    </Stage>
  )
}

/** Platform display, the next train is cancelled and another has moved platform. Design 6. */
export function PlatformDisrupted() {
  const { label } = useClock()

  return (
    <Stage>
      <Header title="Platform 2" time={label} />
      <Banner icon="arrow-lr">The 14:25 to Riverside now leaves from platform 5</Banner>
      <section className="d-hero">
        <div className="d-hero-main">
          <div className="d-hero-line">
            <span className="d-hero-time">14:31</span>
            <span className="d-hero-to">Hill Town</span>
          </div>
          <div className="d-hero-meta">
            <StatusBadge status="cancelled" label="Cancelled" size="lg" />
            <span className="d-dim">signal fault</span>
          </div>
          <p className="d-next-hint">
            <Icon name="arrow-lr" size={30} /> Next train to Hill Town: 15:05 from platform 3
          </p>
        </div>
      </section>
      <MiniRow row={{ time: '14:52', to: 'Riverside', status: 'delayed', label: '+12 late' }} />
      <Footer tone="alert" icon="alert-triangle">
        Signal fault at Mill Lane: delays up to 20 min · updated 14:12
      </Footer>
    </Stage>
  )
}

/** One train on a concourse board. `leaves` is the minute it drops off the board: its time, or a few minutes later if it is cancelled. */
type BoardRow = Row & { place: string; leaves: number }

/**
 * One scenario, two boards. The signal fault at Mill Lane delays the 14:52 out and the 14:58 in, and the 14:31 to Hill Town and the
 * 14:34 from Hill Town are cancelled, because they are the same train there and back.
 */
const departures: BoardRow[] = [
  { time: '14:25', to: 'Riverside', place: '5', status: 'changed', label: 'Platform changed', leaves: at(14, 25) },
  { time: '14:31', to: 'Hill Town', place: '4', status: 'cancelled', label: 'Cancelled', leaves: at(14, 36) },
  { time: '14:40', to: 'Mill Lane', place: '1', status: 'delayed', label: 'Expected 14:47', leaves: at(14, 47) },
  { time: '14:52', to: 'Riverside', place: '1', status: 'delayed', label: '+12 late', leaves: at(15, 4) },
  { time: '15:05', to: 'Hill Town', place: '3', status: 'on_time', label: 'On time', leaves: at(15, 5) },
  { time: '15:20', to: 'Hill Town', place: '5', status: 'on_time', label: 'On time', leaves: at(15, 20) },
  { time: '15:35', to: 'Oak Park', place: '2', status: 'on_time', label: 'On time', leaves: at(15, 35) },
  { time: '15:48', to: 'Riverside', place: '4', status: 'on_time', label: 'On time', leaves: at(15, 48) },
  { time: '16:02', to: 'Mill Lane', place: '1', status: 'on_time', label: 'On time', leaves: at(16, 2) },
  { time: '16:15', to: 'Hill Town', place: '3', status: 'on_time', label: 'On time', leaves: at(16, 15) },
]

const arrivals: BoardRow[] = [
  { time: '14:21', to: 'Riverside', place: '3', status: 'on_time', label: 'On time', leaves: at(14, 21) },
  { time: '14:34', to: 'Hill Town', place: '4', status: 'cancelled', label: 'Cancelled', leaves: at(14, 39) },
  { time: '14:44', to: 'Mill Lane', place: '1', status: 'delayed', label: 'Expected 14:51', leaves: at(14, 51) },
  { time: '14:58', to: 'Riverside', place: '2', status: 'delayed', label: '+12 late', leaves: at(15, 10) },
  { time: '15:10', to: 'Hill Town', place: '3', status: 'on_time', label: 'On time', leaves: at(15, 10) },
  { time: '15:24', to: 'Oak Park', place: '5', status: 'on_time', label: 'On time', leaves: at(15, 24) },
  { time: '15:40', to: 'Riverside', place: '2', status: 'on_time', label: 'On time', leaves: at(15, 40) },
  { time: '15:55', to: 'Mill Lane', place: '1', status: 'on_time', label: 'On time', leaves: at(15, 55) },
  { time: '16:08', to: 'Hill Town', place: '4', status: 'on_time', label: 'On time', leaves: at(16, 8) },
]

const WINDOW = 6
const LEAVE_MS = 700

const upcoming = (all: BoardRow[]) => all.filter((r) => r.leaves > sampleMinutes()).slice(0, WINDOW)

/**
 * The rows a board shows, kept up to date by the clock. A train that has gone fades away and the rows close up; a train that
 * comes on at the bottom fades in. The first draw is not animated here, because the whole screen fades up.
 */
function useBoard(all: BoardRow[]) {
  const [rows, setRows] = useState(() => upcoming(all))
  const [going, setGoing] = useState<ReadonlySet<BoardRow>>(new Set())
  const [fresh, setFresh] = useState<ReadonlySet<BoardRow>>(new Set())
  const busy = useRef(false)
  const shown = useRef(rows)

  useEffect(() => {
    let timer: number | undefined

    const id = window.setInterval(() => {
      if (busy.current) return
      const next = upcoming(all)
      const gone = shown.current.filter((r) => !next.includes(r))

      if (gone.length > 0) {
        busy.current = true
        setGoing(new Set(gone))
        timer = window.setTimeout(() => {
          const after = upcoming(all)
          setFresh(new Set(after.filter((r) => !shown.current.includes(r))))
          shown.current = after
          setRows(after)
          setGoing(new Set())
          busy.current = false
        }, LEAVE_MS)
      } else if (next.length !== shown.current.length) {
        setFresh(new Set(next.filter((r) => !shown.current.includes(r))))
        shown.current = next
        setRows(next)
      }
    }, 500)

    return () => {
      window.clearInterval(id)
      window.clearTimeout(timer)
    }
  }, [all])

  return { rows, going, fresh }
}

const notices = [
  { title: 'Signal fault at Mill Lane', detail: 'Delays up to 20 min · updated 14:12' },
  { title: 'Lift out of service, Oak Park', detail: 'Step-free route via platform 1' },
]

function Board({ title, place, all }: { title: string; place: string; all: BoardRow[] }) {
  const { label } = useClock()
  const { rows, going, fresh } = useBoard(all)

  return (
    <Stage>
      <Header title={title} time={label} />
      <div className="d-concourse">
        <div className="d-table">
          <div className="d-th">
            <span>Time</span>
            <span>{place}</span>
            <span>Plat</span>
            <span>Status</span>
          </div>
          {rows.map((r) => (
            <div
              key={r.time + r.to}
              className={`d-tr ${r.status === 'cancelled' ? 'd-tr-cancelled' : ''} ${going.has(r) ? 'd-tr-leaving' : ''} ${fresh.has(r) ? 'rise' : ''}`}
            >
              <span className="d-tr-time">{r.time}</span>
              <span>{r.to}</span>
              <span className="d-tr-plat">{r.place}</span>
              <StatusBadge status={r.status} label={r.label} />
            </div>
          ))}
        </div>
        <aside className="d-updates">
          <h2>Service updates</h2>
          {notices.map((n) => (
            <div key={n.title} className="d-update">
              <Icon name="alert-triangle" size={28} />
              <div>
                <strong>{n.title}</strong>
                <span>{n.detail}</span>
              </div>
            </div>
          ))}
        </aside>
      </div>
      <Footer tone="live" icon="check-circle">
        Live · updated {label}
      </Footer>
    </Stage>
  )
}

/** The concourse departures board. Design 8. */
export function Departures() {
  return <Board title="Departures · Central" place="Destination" all={departures} />
}

/** The concourse arrivals board: the same layout, showing where each train is coming from. */
export function Arrivals() {
  return <Board title="Arrivals · Central" place="From" all={arrivals} />
}

