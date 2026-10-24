import { useEffect, useState, type ReactNode } from 'react'
import { Icon } from '../icons.tsx'
import { Banner, Footer, Header, Stage, StatusBadge } from '../parts.tsx'
import { getJson } from './api.ts'
import { rowLabel, words } from './i18n.ts'
import { useSmoothRows } from './useSmoothRows.ts'
import { PAGE_SECONDS, arrivalRow, clock, departureRow, minutesUntil, noticesFor, pagesOf, platformModel, type Link, type Row } from './logic.ts'
import type { JourneyDetail, Notice } from './types.ts'
import type { StationState } from './useStation.ts'

type Ready = Extract<StationState, { kind: 'ready' }>

/** Every fixed word is shown in two languages at once: English first, and the Sinhala in a smaller line under it. */
const en = words.en
const si = words.si

/** The words of the footer, which is where a display is honest about how fresh its information is (REQ-DSP-004). */
function LiveFooter({ ready, notice, right }: { ready: Ready; notice?: Notice; right?: ReactNode }) {
  const { data, link } = ready
  const updated = clock(data.answeredServerMs, data.timeZone)

  if (link === 'offline') {
    return (
      <Footer tone="stale" icon="wifi-off" sub={si.noUpdateSince(updated)} right={right}>
        {en.noUpdateSince(updated)}
      </Footer>
    )
  }
  if (link === 'delayed') {
    return (
      <Footer tone="stale" icon="wifi-off" sub={si.updatedUnavailable(updated)} right={right}>
        {en.updatedUnavailable(updated)}
      </Footer>
    )
  }
  if (notice) {
    // The notice is shown as the controller wrote it, in one line.
    return (
      <Footer tone="alert" icon="alert-triangle" right={right}>
        {notice.title} · {clock(notice.updatedAtUtc, data.timeZone)}
      </Footer>
    )
  }
  return (
    <Footer tone="live" icon="check-circle" sub={si.live(updated)} right={right}>
      {en.live(updated)}
    </Footer>
  )
}

/** Shown above everything else when the display has no live data, as the platform displays of the design do. */
function OfflineBanner({ link }: { link: Link }) {
  if (link !== 'offline') return null
  return (
    <Banner icon="volume" tone="stale" sub={si.unavailableBanner}>
      {en.unavailableBanner}
    </Banner>
  )
}

/** What the journey calls at after this station, for the big train on the platform display. Station names stay as they are. */
function useCallingAt(journeyId: string | undefined, stationCode: string): string[] {
  const [calling, setCalling] = useState<{ id: string; stops: string[] } | null>(null)

  useEffect(() => {
    if (!journeyId) return
    let cancelled = false
    getJson<JourneyDetail>(`/api/journeys/${journeyId}`)
      .then((journey) => {
        if (cancelled) return
        const here = journey.stops.find((s) => s.stationCode === stationCode)?.sequence ?? 0
        setCalling({ id: journeyId, stops: journey.stops.filter((s) => s.sequence > here).map((s) => s.stationName) })
      })
      .catch(() => {
        // Without the stops the line is simply left out.
      })
    return () => {
      cancelled = true
    }
  }, [journeyId, stationCode])

  return calling && calling.id === journeyId ? calling.stops : []
}

/** A train on a later day is marked "Tomorrow" in both languages, so a morning train is not taken for one leaving now. */
function Tomorrow({ pill }: { pill?: boolean }) {
  return pill ? (
    <span className="d-day-pill">
      {en.tomorrow} · {si.tomorrow}
    </span>
  ) : (
    <small className="d-day">
      {en.tomorrow} · {si.tomorrow}
    </small>
  )
}

function MiniRow({ row }: { row: Row }) {
  return (
    <div className="d-mini">
      <span className="d-mini-time">
        {row.time}
        {row.tomorrow && <Tomorrow />}
      </span>
      <span className="d-mini-to">{row.name}</span>
      <StatusBadge status={row.status} label={rowLabel('en', row)} sub={rowLabel('si', row)} />
    </div>
  )
}

/** The platform display: the next train large with a countdown, the two after it small, and the notice that matters at the top. */
export function LivePlatform({ ready, platform }: { ready: Ready; platform: string | null }) {
  const { data, link, now } = ready
  const code = data.stationCode
  const ctx = { link, timeZone: data.timeZone, nowMs: now }
  const model = platformModel(data.departures, platform)
  const notices = noticesFor(data.notices, code)
  const urgent = notices.find((n) => n.severity !== 'Minor')
  const next = model.next
  const calling = useCallingAt(next?.journeyId, code)
  const title = platform ? `${data.stationName} · ${en.platform(platform)}` : data.stationName
  const sub = platform ? si.platform(platform) : undefined

  if (!next) {
    return (
      <Stage>
        <Header title={title} sub={sub} time={clock(now, data.timeZone)} />
        <OfflineBanner link={link} />
        <section className="d-message">
          <h2>{en.noMoreTrains}</h2>
          <p>{en.noMoreTrainsHint}</p>
          <h2 className="d-message-sub">{si.noMoreTrains}</h2>
          <p>{si.noMoreTrainsHint}</p>
        </section>
        <LiveFooter ready={ready} notice={notices[0]} />
      </Stage>
    )
  }

  const row = departureRow(next, ctx)
  const cancelled = next.status === 'Cancelled'
  const minutes = minutesUntil(Date.parse(next.expectedDepartureUtc), now)
  const showCountdown = !cancelled && link !== 'offline' && minutes <= 60
  const alt = model.alternative

  return (
    <Stage>
      <Header title={title} sub={sub} time={clock(now, data.timeZone)} />
      <OfflineBanner link={link} />
      {link !== 'offline' && urgent && <Banner icon="alert-triangle">{urgent.title}</Banner>}
      <section className="d-hero">
        <div className="d-hero-main">
          <div className="d-hero-line">
            <span className="d-hero-time">{row.time}</span>
            <span className="d-hero-to">{row.name}</span>
            {row.tomorrow && <Tomorrow pill />}
          </div>
          {!cancelled && calling.length > 0 && (
            <p className="d-calling">
              {en.callingLabel} · {si.callingLabel}: {calling.join(', ')}
            </p>
          )}
          <div className="d-hero-meta">
            <StatusBadge status={row.status} label={rowLabel('en', row)} sub={rowLabel('si', row)} size="lg" />
            {row.platform !== '–' && !platform && (
              <span className="d-dim">
                {en.platform(row.platform)} · {si.platform(row.platform)}
              </span>
            )}
          </div>
          {cancelled && alt && (
            <p className="d-next-hint">
              <Icon name="arrow-lr" size={30} />
              <span>
                {en.nextTrain(alt.destinationName, clock(alt.scheduledDepartureUtc, data.timeZone), alt.platform)}
                <br />
                {si.nextTrain(alt.destinationName, clock(alt.scheduledDepartureUtc, data.timeZone), alt.platform)}
              </span>
            </p>
          )}
        </div>
        {showCountdown && (
          <div className="d-countdown">
            {minutes > 0 ? (
              <>
                <strong>{minutes}</strong>
                <span>
                  {en.min} · {si.min}
                </span>
              </>
            ) : (
              <strong className="d-countdown-now">
                {en.now} · {si.now}
              </strong>
            )}
          </div>
        )}
      </section>
      {model.after.map((item) => (
        <MiniRow key={item.journeyId} row={departureRow(item, ctx)} />
      ))}
      <LiveFooter ready={ready} notice={notices[0]} />
    </Stage>
  )
}

/** The concourse boards: departures or arrivals, six rows to a page (more rows turn over by themselves), and the service updates beside them. */
export function LiveBoard({ ready, kind }: { ready: Ready; kind: 'departures' | 'arrivals' }) {
  const { data, link, now } = ready
  const ctx = { link, timeZone: data.timeZone, nowMs: now }
  const target: Row[] = kind === 'departures' ? data.departures.map((d) => departureRow(d, ctx)) : data.arrivals.map((a) => arrivalRow(a, ctx))
  // A train that has gone fades away and the rows close up; a new one fades in (see useSmoothRows).
  const { shown: all, going, fresh } = useSmoothRows(target)
  const pages = pagesOf(all)
  const notices = noticesFor(data.notices, data.stationCode)

  // A board with more rows than fit turns over a page at a time, while the screen is up.
  const [page, setPage] = useState(0)
  useEffect(() => {
    if (pages.length < 2) return
    const timer = setInterval(() => setPage((p) => p + 1), PAGE_SECONDS * 1000)
    return () => clearInterval(timer)
  }, [pages.length])

  const pageIndex = pages.length === 0 ? 0 : page % pages.length
  const shown = pages[pageIndex] ?? []
  const heading = kind === 'departures' ? en.departures : en.arrivals
  const headingSi = kind === 'departures' ? si.departures : si.arrivals

  return (
    <Stage>
      <Header title={`${heading} · ${data.stationName}`} sub={headingSi} time={clock(now, data.timeZone)} />
      <OfflineBanner link={link} />
      <div className="d-concourse">
        <div className="d-table">
          <div className="d-th">
            <span>
              {en.time}
              <small>{si.time}</small>
            </span>
            <span>
              {kind === 'departures' ? en.destination : en.from}
              <small>{kind === 'departures' ? si.destination : si.from}</small>
            </span>
            <span>
              {en.platformShort}
              <small>{si.platformShort}</small>
            </span>
            <span>
              {en.status}
              <small>{si.status}</small>
            </span>
          </div>
          {shown.length === 0 && (
            <div className="d-tr d-tr-empty">
              <span className="d-row-note">
                {kind === 'departures' ? en.noMoreDepartures : en.noMoreArrivals} · {kind === 'departures' ? si.noMoreDepartures : si.noMoreArrivals}
              </span>
            </div>
          )}
          {/* A new page fades in as a whole when a long board turns over. */}
          <div key={pageIndex} className={pageIndex > 0 || pages.length > 1 ? 'd-page-in' : undefined}>
            {shown.map((r) => (
              <div
                key={r.key}
                className={`d-tr ${r.status === 'cancelled' ? 'd-tr-cancelled' : ''} ${going.has(r.key) ? 'd-tr-leaving' : ''} ${fresh.has(r.key) ? 'rise' : ''}`}
              >
                <span className="d-tr-time">
                  {r.time}
                  {r.tomorrow && <Tomorrow />}
                </span>
                <span>{r.name}</span>
                <span className="d-tr-plat">{r.platform}</span>
                <StatusBadge status={r.status} label={rowLabel('en', r)} sub={rowLabel('si', r)} />
              </div>
            ))}
          </div>
        </div>
        {/* The service updates only take room while there is something to say; otherwise the board uses the full width. */}
        {notices.length > 0 && (
          <aside className="d-updates">
            <h2>
              {en.serviceUpdates}
              <small className="d-h2-sub">{si.serviceUpdates}</small>
            </h2>
            {notices.slice(0, 3).map((n) => (
              <div key={n.id} className="d-update">
                <Icon name="alert-triangle" size={28} />
                <div>
                  <strong>{n.title}</strong>
                  <span>{n.message.length > 70 ? `${n.message.slice(0, 67)}…` : n.message}</span>
                </div>
              </div>
            ))}
          </aside>
        )}
      </div>
      <LiveFooter ready={ready} right={pages.length > 1 ? <span className="d-page">{en.page(pageIndex + 1, pages.length)}</span> : undefined} />
    </Stage>
  )
}

/** A full screen of the notices the controller has published for this station, so they are read and not just glanced at. */
export function LiveNotices({ ready }: { ready: Ready }) {
  const { data, link, now } = ready
  const notices = noticesFor(data.notices, data.stationCode).slice(0, 3)

  return (
    <Stage>
      <Header title={`${en.serviceUpdates} · ${data.stationName}`} sub={si.serviceUpdates} time={clock(now, data.timeZone)} />
      <OfflineBanner link={link} />
      <section className="d-notice-list">
        {notices.map((n) => (
          <div key={n.id} className={`d-notice d-notice-${n.severity.toLowerCase()}`}>
            <Icon name={n.severity === 'Severe' ? 'x-circle' : 'alert-triangle'} size={44} />
            <div>
              <h3>{n.title}</h3>
              <p>{n.message.length > 180 ? `${n.message.slice(0, 177)}…` : n.message}</p>
            </div>
          </div>
        ))}
      </section>
      <LiveFooter ready={ready} />
    </Stage>
  )
}

/** The screens before there is anything to show: connecting, no answer yet, or a station that does not exist. */
export function StatusScreen({ kind }: { kind: 'loading' | 'unavailable' | 'unknown-station' }) {
  const pick = (w: typeof en) => ({ loading: w.connecting, unavailable: w.unavailable, 'unknown-station': w.unknownStation })[kind]
  const first = pick(en)
  const second = pick(si)

  return (
    <Stage>
      <Header title="SmartRail" time="" />
      <section className="d-message">
        <h2>{first.title}</h2>
        <p>{first.detail}</p>
        <h2 className="d-message-sub">{second.title}</h2>
        <p>{second.detail}</p>
      </section>
      <Footer tone="stale" icon="wifi-off" sub={second.footer}>
        {first.footer}
      </Footer>
    </Stage>
  )
}
