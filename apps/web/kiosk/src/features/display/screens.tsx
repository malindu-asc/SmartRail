import type { ReactNode } from 'react'
import { Icon } from './icons'
import { Banner, Footer, Header, Stage, StatusBadge, type Status } from './parts'

/**
 * The station displays at high fidelity. Nothing here is wired to the API yet: the numbers are sample data so the design can be
 * judged. A real display reads the next departures for its platform or station and uses server time, never the device clock.
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
          <p className="d-calling">Calling at Oak Park, Mill Lane, Riverside</p>
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
      <Banner tone="warn" icon="arrow-lr">
        The 14:25 to Riverside now leaves from platform 5
      </Banner>
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

/** Platform display after the live feed has been lost for five minutes: scheduled times only. Design 7. */
export function PlatformNoLive() {
  return (
    <Stage>
      <Header title="Platform 2" time="14:18" />
      <Banner tone="stale" icon="volume">
        Live information unavailable. Listen for announcements.
      </Banner>
      <section className="d-hero">
        <div className="d-hero-main">
          <div className="d-hero-line">
            <span className="d-hero-time">14:25</span>
            <span className="d-hero-to">Riverside</span>
          </div>
          <div className="d-hero-meta">
            <StatusBadge status="scheduled" label="Scheduled time" size="lg" />
            <span className="d-dim">not a live estimate</span>
          </div>
        </div>
      </section>
      <MiniRow row={{ time: '14:52', to: 'Riverside', status: 'scheduled', label: 'Scheduled' }} />
      <Footer tone="stale" icon="wifi-off">
        No update since 14:06
      </Footer>
    </Stage>
  )
}

/** Platform display when nothing else runs today. Added: the board had no end-of-service state. */
export function PlatformEndOfService() {
  return (
    <Stage>
      <Header title="Platform 2" time="23:48" />
      <section className="d-center">
        <h2>No more trains today</h2>
        <p>
          The first train tomorrow is the <strong>05:40</strong> to Riverside.
        </p>
      </section>
      <Footer tone="live" icon="check-circle">
        Live · updated 23:48
      </Footer>
    </Stage>
  )
}

const board: (Row & { plat: string })[] = [
  { time: '14:25', to: 'Riverside', plat: '5', status: 'changed', label: 'Platform changed' },
  { time: '14:31', to: 'Hill Town', plat: '4', status: 'cancelled', label: 'Cancelled' },
  { time: '14:40', to: 'Mill Lane', plat: '1', status: 'delayed', label: 'Expected 14:47' },
  { time: '14:52', to: 'Riverside', plat: '1', status: 'delayed', label: '+12 late' },
  { time: '15:05', to: 'Hill Town', plat: '3', status: 'on_time', label: 'On time' },
  { time: '15:20', to: 'Hill Town', plat: '5', status: 'on_time', label: 'On time' },
]

/** The concourse board: every departure, the notices to the right. Design 8. */
export function Concourse({ stale = false }: { stale?: boolean }) {
  const rows: (Row & { plat: string })[] = stale
    ? board.map((r) => ({ ...r, status: 'scheduled', label: 'Scheduled' }))
    : board

  return (
    <Stage>
      <Header title="Departures · Central" time="14:18" />
      {stale && (
        <Banner tone="stale" icon="volume">
          Live information unavailable. Times shown are the timetable. Listen for announcements.
        </Banner>
      )}
      <div className="d-concourse">
        <div className="d-table">
          <div className="d-th">
            <span>Time</span>
            <span>Destination</span>
            <span>Plat</span>
            <span>Status</span>
          </div>
          {rows.map((r) => (
            <div key={r.time + r.to} className={`d-tr ${r.status === 'cancelled' ? 'd-tr-cancelled' : ''}`}>
              <span className="d-tr-time">{r.time}</span>
              <span>{r.to}</span>
              <span className="d-tr-plat">{r.plat}</span>
              <StatusBadge status={r.status} label={r.label} />
            </div>
          ))}
        </div>
        {!stale && (
          <aside className="d-updates">
            <h2>Service updates</h2>
            <Update title="Signal fault at Mill Lane" detail="Delays up to 20 min · updated 14:12" />
            <Update title="Lift out of service, Oak Park" detail="Step-free route via platform 1" />
          </aside>
        )}
      </div>
      {stale ? (
        <Footer tone="stale" icon="wifi-off">
          No update since 14:06
        </Footer>
      ) : (
        <Footer tone="live" icon="check-circle" right="Page 1 of 2">
          Live · updated 14:18
        </Footer>
      )}
    </Stage>
  )
}

function Update({ title, detail }: { title: string; detail: ReactNode }) {
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

/** The station is closed: a full screen notice and the way on (REQ-LIVE-016). Added. */
export function StationClosed() {
  return (
    <Stage>
      <Header title="Central" time="14:18" />
      <section className="d-center d-center-alert">
        <Icon name="x-circle" size={120} />
        <h2>This station is closed</h2>
        <p>No trains are stopping here. Ask a member of staff for another way to travel.</p>
        <p className="d-dim">Closed since 13:40 · updated 14:18</p>
      </section>
      <Footer tone="alert" icon="alert-triangle">
        Staff are on the concourse to help
      </Footer>
    </Stage>
  )
}

/** A new or reset display waiting for a technician to pair it (UC-DSP-04). Added. */
export function Pairing() {
  return (
    <Stage>
      <header className="d-header">
        <h1>SmartRail</h1>
      </header>
      <section className="d-center">
        <Icon name="lock" size={96} />
        <h2>This display is not set up</h2>
        <p>A technician enters this code in the SmartRail console.</p>
        <div className="d-code">482 913</div>
        <p className="d-dim">The code changes every 10 minutes</p>
      </section>
      <Footer tone="stale" icon="wifi-off">
        Waiting for set up
      </Footer>
    </Stage>
  )
}
