import { Card, CardHeader } from '@/shared/components/ui'
import { formatTime } from '@/shared/utils'
import type { StopView } from '../types'

/** The state words the console gets for a stop: Done (left it), At stop (arrived, not left), Next, Later or Cancelled. */
const DONE = 'Done'
const HERE = 'At stop'

/** Where the train is, from the stops it has reported: at a stop, between two stops, not started or finished. */
function position(stops: StopView[]): { text: string; at: number | null } {
  const real = stops.filter((s) => s.kind !== 'Passing' && s.state !== 'Cancelled' && s.state !== 'Skipped')
  const last = real[real.length - 1]
  const here = real.find((s) => s.state === HERE)
  const left = real.filter((s) => s.state === DONE)

  if (last?.state === HERE) return { text: `Arrived at ${last.stationName}`, at: last.sequence }
  if (here) return { text: `At ${here.stationName}`, at: here.sequence }
  const from = left[left.length - 1]
  if (from) {
    const next = real.find((s) => s.sequence > from.sequence)
    return { text: next ? `Between ${from.stationName} and ${next.stationName}` : `Left ${from.stationName}`, at: null }
  }
  return { text: 'Not started', at: null }
}

function time(stop: StopView): { label: string; note: string } {
  const done = stop.actualDepartureUtc ?? stop.actualArrivalUtc
  if (done) return { label: formatTime(done), note: stop.state === DONE ? 'Departed' : 'Arrived' }
  // No live estimate (no recent driver update): show the timetable time and say so, never a guess dressed up as an estimate.
  const expected = stop.expectedArrivalUtc ?? stop.expectedDepartureUtc
  if (expected) return { label: formatTime(expected), note: 'Expected' }
  return { label: formatTime(stop.scheduledArrivalUtc ?? stop.scheduledDepartureUtc), note: 'Scheduled' }
}

/** A vertical line of the stops: passed stops filled, the train's place marked, and the next ones with their expected time. */
export function JourneyProgress({ stops }: { stops: StopView[] }) {
  const shown = stops.filter((s) => s.kind !== 'Passing')
  const where = position(stops)

  return (
    <Card>
      <CardHeader title="Where the train is" />
      <div className="px-5 pb-4">
        <p className="mb-3 text-lg font-bold">{where.text}</p>
        <ol className="space-y-0">
          {shown.map((stop, index) => {
            const passed = stop.state === DONE || stop.state === 'Skipped'
            const here = stop.sequence === where.at
            const cancelled = stop.state === 'Cancelled'
            const skipped = stop.state === 'Skipped'
            const t = time(stop)
            return (
              <li key={stop.sequence} className="relative flex gap-4 pb-4 last:pb-0">
                {index < shown.length - 1 && <span aria-hidden className={`absolute left-[7px] top-4 h-full w-0.5 ${passed ? 'bg-on-time' : 'bg-line'}`} />}
                <span
                  aria-hidden
                  className={`z-10 mt-1 size-4 shrink-0 rounded-full border-2 ${here ? 'border-on-time bg-on-time ring-4 ring-on-time/20' : passed ? 'border-on-time bg-on-time' : cancelled ? 'border-cancelled bg-surface' : 'border-line-strong bg-surface'}`}
                />
                <div className="flex flex-1 items-baseline justify-between gap-3">
                  <div>
                    <p className={`font-semibold ${cancelled ? 'text-fg-tertiary line-through' : ''}`}>
                      {stop.stationName}
                      {here && <span className="ml-2 rounded-full bg-on-time/15 px-2 py-0.5 text-xs font-bold text-on-time">Train is here</span>}
                    </p>
                    <p className="text-fg-secondary">
                      {stop.platform ? `Platform ${stop.platform}` : 'No platform yet'}
                      {stop.originalPlatform && stop.originalPlatform !== stop.platform && <span className="ml-1 text-changed">(was {stop.originalPlatform})</span>}
                    </p>
                  </div>
                  <p className="text-right">
                    <span className="block font-bold tabular-nums">{cancelled ? '–' : t.label}</span>
                    <span className="block text-xs text-fg-secondary">{cancelled ? 'Cancelled' : skipped ? 'Not stopping' : t.note}</span>
                  </p>
                </div>
              </li>
            )
          })}
        </ol>
      </div>
    </Card>
  )
}
