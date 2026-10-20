import { useEffect, useState } from 'react'
import { Icon } from './icons'
import { Banner, Footer, Header, Rise, Stage, StatusBadge, type Status } from './parts'

/**
 * The station displays at high fidelity. Nothing here is wired to the API yet: the numbers are sample data so the design can be
 * judged. A real display reads the next departures for its platform or station and uses server time, never the device clock.
 *
 * Each screen brings its parts in one after another (see Rise), the most important first.
 */

type Row = { time: string; to: string; status: Status; label: string }

function MiniRow({ row, delay }: { row: Row; delay: number }) {
  return (
    <Rise className="d-mini" delay={delay}>
      <span className="d-mini-time">{row.time}</span>
      <span className="d-mini-to">{row.to}</span>
      <StatusBadge status={row.status} label={row.label} />
    </Rise>
  )
}

/** Platform display, a train on time: the next train large, the two after it small. Design 5. */
export function PlatformNext() {
  return (
    <Stage>
      <Header title="Platform 2" time="14:18" />
      <section className="d-hero">
        <div className="d-hero-main">
          <Rise delay={150} className="d-hero-line">
            <span className="d-hero-time">14:25</span>
            <span className="d-hero-to">Riverside</span>
          </Rise>
          <Rise delay={450}>
            <p className="d-calling">Calling at Oak Park, Mill Lane, Riverside</p>
          </Rise>
          <Rise delay={700} className="d-hero-meta">
            <StatusBadge status="on_time" label="On time" size="lg" />
            <span className="d-dim">4 cars</span>
          </Rise>
        </div>
        <Rise delay={900} className="d-countdown">
          <strong>7</strong>
          <span>min</span>
        </Rise>
      </section>
      <MiniRow row={{ time: '14:52', to: 'Riverside', status: 'on_time', label: 'On time' }} delay={1400} />
      <MiniRow row={{ time: '15:20', to: 'Riverside', status: 'on_time', label: 'On time' }} delay={1900} />
      <Footer tone="live" icon="check-circle" delay={2400}>
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
          <Rise delay={700} className="d-hero-line">
            <span className="d-hero-time">14:31</span>
            <span className="d-hero-to">Hill Town</span>
          </Rise>
          <Rise delay={1100} className="d-hero-meta">
            <StatusBadge status="cancelled" label="Cancelled" size="lg" />
            <span className="d-dim">signal fault</span>
          </Rise>
          <Rise delay={1600}>
            <p className="d-next-hint">
              <Icon name="arrow-lr" size={30} /> Next train to Hill Town: 15:05 from platform 3
            </p>
          </Rise>
        </div>
      </section>
      <MiniRow row={{ time: '14:52', to: 'Riverside', status: 'delayed', label: '+12 late' }} delay={2100} />
      <Footer tone="alert" icon="alert-triangle" delay={2600}>
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
            <Rise
              key={r.time + r.to}
              // The first time the board shows, the rows come in one after another; later only the new row rises.
              delay={first === 0 ? 300 + i * 450 : 0}
              className={`d-tr ${r.status === 'cancelled' ? 'd-tr-cancelled' : ''} ${i === 0 && leaving ? 'd-tr-leaving' : ''}`}
            >
              <span className="d-tr-time">{r.time}</span>
              <span>{r.to}</span>
              <span className="d-tr-plat">{r.plat}</span>
              <StatusBadge status={r.status} label={r.label} />
            </Rise>
          ))}
        </div>
        <aside className="d-updates">
          <Rise delay={3200}>
            <h2>Service updates</h2>
          </Rise>
          <Rise delay={3500}>
            <Update title="Signal fault at Mill Lane" detail="Delays up to 20 min · updated 14:12" />
          </Rise>
          <Rise delay={3900}>
            <Update title="Lift out of service, Oak Park" detail="Step-free route via platform 1" />
          </Rise>
        </aside>
      </div>
      <Footer tone="live" icon="check-circle" right="Page 1 of 2" delay={4300}>
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
