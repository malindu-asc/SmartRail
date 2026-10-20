import { useEffect, useState } from 'react'
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
  return (
    <Stage>
      <Header title="Platform 2" time="14:18" />
      <section className="d-hero">
        <div className="d-hero-main">
          <div className="d-hero-line">
            <span className="d-hero-time">14:25</span>
            <span className="d-hero-to">Riverside</span>
          </div>
          <div>
            <p className="d-calling">Calling at Oak Park, Mill Lane, Riverside</p>
          </div>
          <div className="d-hero-meta">
            <StatusBadge status="on_time" label="On time" size="lg" />
            <span className="d-dim">4 cars</span>
          </div>
        </div>
        <div className="d-countdown">
          <strong>7</strong>
          <span>min</span>
        </div>
      </section>
      <MiniRow row={{ time: '14:52', to: 'Riverside', status: 'on_time', label: 'On time' }} />
      <MiniRow row={{ time: '15:20', to: 'Riverside', status: 'on_time', label: 'On time' }} />
      <Footer tone="live" icon="check-circle">
        Live · updated 14:18
      </Footer>
    </Stage>
  )
}

/** Platform display, the next train is cancelled and another has moved platform. Design 6. */
export function PlatformDisrupted() {
  return (
    <Stage>
      <Header title="Platform 2" time="14:18" />
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
          <div>
            <p className="d-next-hint">
              <Icon name="arrow-lr" size={30} /> Next train to Hill Town: 15:05 from platform 3
            </p>
          </div>
        </div>
      </section>
      <MiniRow row={{ time: '14:52', to: 'Riverside', status: 'delayed', label: '+12 late' }} />
      <Footer tone="alert" icon="alert-triangle">
        Signal fault at Mill Lane: delays up to 20 min · updated 14:12
      </Footer>
    </Stage>
  )
}

type BoardRow = Row & { plat: string }

/** The day's trains from this station. The board shows six at a time and moves up as each one leaves. */
const departures: BoardRow[] = [
  { time: '14:25', to: 'Riverside', plat: '5', status: 'changed', label: 'Platform changed' },
  { time: '14:31', to: 'Hill Town', plat: '4', status: 'cancelled', label: 'Cancelled' },
  { time: '14:40', to: 'Mill Lane', plat: '1', status: 'delayed', label: 'Expected 14:47' },
  { time: '14:52', to: 'Riverside', plat: '1', status: 'delayed', label: '+12 late' },
  { time: '15:05', to: 'Hill Town', plat: '3', status: 'on_time', label: 'On time' },
  { time: '15:20', to: 'Hill Town', plat: '5', status: 'on_time', label: 'On time' },
  { time: '15:35', to: 'Oak Park', plat: '2', status: 'on_time', label: 'On time' },
  { time: '15:48', to: 'Riverside', plat: '4', status: 'on_time', label: 'On time' },
  { time: '16:02', to: 'Mill Lane', plat: '1', status: 'on_time', label: 'On time' },
]

const WINDOW = 6
const BOARD_UPDATE_MS = 7000
const LEAVE_MS = 700

/**
 * The concourse board: every departure, the notices to the right. Design 8. The rows come in one at a time. After a few seconds the
 * first train leaves: its row fades away and closes up, and the next train fades in at the bottom.
 */
export function Concourse() {
  const [first, setFirst] = useState(0)
  const [leaving, setLeaving] = useState(false)

  useEffect(() => {
    if (first >= 2 || leaving) return
    const timer = window.setTimeout(() => setLeaving(true), BOARD_UPDATE_MS)
    return () => window.clearTimeout(timer)
  }, [first, leaving])

  useEffect(() => {
    if (!leaving) return
    const timer = window.setTimeout(() => {
      setFirst((n) => n + 1)
      setLeaving(false)
    }, LEAVE_MS)
    return () => window.clearTimeout(timer)
  }, [leaving])

  const rows = departures.slice(first, first + WINDOW)
  const clock = first === 0 ? '14:18' : first === 1 ? '14:19' : '14:20'

  return (
    <Stage>
      <Header title="Departures · Central" time={clock} />
      <div className="d-concourse">
        <div className="d-table">
          <div className="d-th">
            <span>Time</span>
            <span>Destination</span>
            <span>Plat</span>
            <span>Status</span>
          </div>
          {/* Keyed by train, so a row that stays is not redrawn and one that arrives fades in. */}
          {rows.map((r, i) => (
            <div
              key={r.time + r.to}
              // The board appears with the screen; only a train that arrives later fades in on its own.
              className={`d-tr ${r.status === 'cancelled' ? 'd-tr-cancelled' : ''} ${i === 0 && leaving ? 'd-tr-leaving' : ''} ${first > 0 && i === WINDOW - 1 ? 'rise' : ''}`}
            >
              <span className="d-tr-time">{r.time}</span>
              <span>{r.to}</span>
              <span className="d-tr-plat">{r.plat}</span>
              <StatusBadge status={r.status} label={r.label} />
            </div>
          ))}
        </div>
        <aside className="d-updates">
          <div>
            <h2>Service updates</h2>
          </div>
          <div>
            <Update title="Signal fault at Mill Lane" detail="Delays up to 20 min · updated 14:12" />
          </div>
          <div>
            <Update title="Lift out of service, Oak Park" detail="Step-free route via platform 1" />
          </div>
        </aside>
      </div>
      <Footer tone="live" icon="check-circle" right="Page 1 of 2">
        Live · updated {clock}
      </Footer>
    </Stage>
  )
}

function Update({ title, detail }: { title: string; detail: string }) {
  return (
    <div className="d-update">
      <Icon name="alert-triangle" size={28} />
      <div>
        <strong>{title}</strong>
        <span>{detail}</span>
      </div>
    </div>
  )
}
