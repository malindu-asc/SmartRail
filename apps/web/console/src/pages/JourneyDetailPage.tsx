import { ArrowLeft } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { EmergencyTerminateModal } from '@/features/incidents'
import { DataCell, JourneyActions, JourneyProgress, JourneyStatus, PlatformCell, StopsTable, useJourney, useJourneyEvents } from '@/features/journeys'
import { SendMessageModal } from '@/features/messages'
import { AssignModal } from '@/features/planning'
import { Card, CardHeader, CardSkeleton, ErrorState, PageHeader, TableSkeleton } from '@/shared/components/ui'
import { formatDateTime, formatDelay, formatTime } from '@/shared/utils'

/** One journey: where it is, its stops, crew, history and the actions the person may take. */
export function JourneyDetailPage() {
  const { id = '' } = useParams()
  const journey = useJourney(id)
  const events = useJourneyEvents(id)
  const [dialog, setDialog] = useState<string | null>(null)

  if (journey.isError) return <ErrorState error={journey.error} onRetry={() => void journey.refetch()} />

  const detail = journey.data
  const row = detail?.journey

  return (
    <>
      <Link to="/live-board" className="mb-3 inline-flex items-center gap-1.5 text-fg-secondary hover:text-fg"><ArrowLeft aria-hidden className="size-4" />Live board</Link>
      <PageHeader
        title={row ? `${row.trainNumber}: ${row.originName} to ${row.destinationName}` : 'Journey'}
        description={row ? `Departs ${formatTime(row.scheduledDepartureUtc)} · ${row.routeCode}` : undefined}
        actions={row && <JourneyStatus journey={row} />}
      />

      {!detail || !row ? (
        <div className="grid gap-4 xl:grid-cols-[1.6fr_1fr]"><TableSkeleton rows={7} columns={6} label="Loading the journey" /><CardSkeleton lines={5} /></div>
      ) : (
        <div className="grid gap-4 xl:grid-cols-[1.6fr_1fr]">
          <div className="space-y-4">
            <Card>
              <dl className="grid grid-cols-2 gap-x-6 gap-y-4 px-5 py-4 md:grid-cols-4">
                <Fact label="Next stop">{row.nextStopName ?? '–'}</Fact>
                <Fact label="Platform"><PlatformCell journey={row} /></Fact>
                <Fact label="Delay">{formatDelay(row.delayMinutes)}</Fact>
                <Fact label="Data"><DataCell journey={row} /></Fact>
                <Fact label="Driver">{detail.crew.driverName ?? '–'}</Fact>
                <Fact label="Unit">{detail.crew.unitCode ? `${detail.crew.unitCode} ${detail.crew.unitName ?? ''}` : '–'}</Fact>
                <Fact label="Last driver event">{detail.crew.lastDriverEvent ?? '–'}</Fact>
                <Fact label="Duty">{detail.crew.dutyFromUtc ? `${formatTime(detail.crew.dutyFromUtc)} to ${formatTime(detail.crew.dutyToUtc)}` : '–'}</Fact>
              </dl>
              {(detail.cancelReason || detail.endReason || detail.controllerNote) && (
                <p className="border-t border-line px-5 py-3 text-fg-secondary">
                  {[detail.cancelReason && `Cancelled: ${detail.cancelReason}`, detail.endReason && `Ended: ${detail.endReason}`, detail.controllerNote].filter(Boolean).join(' · ')}
                </p>
              )}
            </Card>

            <JourneyProgress stops={detail.stops} />

            <StopsTable stops={detail.stops} />

            <Card>
              <CardHeader title="History" />
              {events.isPending ? (
                <p className="px-5 py-4 text-fg-tertiary">Loading…</p>
              ) : (events.data ?? []).length === 0 ? (
                <p className="px-5 py-4 text-fg-secondary">No events yet.</p>
              ) : (
                <ul className="divide-y divide-line">
                  {(events.data ?? []).map((event) => (
                    <li key={event.id} className="flex items-start justify-between gap-4 px-5 py-3">
                      <div>
                        <p className="font-semibold">{event.text}</p>
                        <p className="text-xs text-fg-tertiary">{event.source}{event.note && ` · ${event.note}`}</p>
                      </div>
                      <span className="whitespace-nowrap text-fg-secondary">{formatDateTime(event.occurredAtUtc)}</span>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          </div>

          <div className="space-y-4">
            <JourneyActions detail={detail} onExternal={setDialog} />
          </div>

          <AssignModal journeyId={row.journeyId} open={dialog === 'assign'} onClose={() => setDialog(null)} />
          <SendMessageModal open={dialog === 'message_driver'} onClose={() => setDialog(null)} journeyId={row.journeyId} />
          <EmergencyTerminateModal journeyId={row.journeyId} trainNumber={row.trainNumber} open={dialog === 'emergency_terminate'} onClose={() => setDialog(null)} />
        </div>
      )}
    </>
  )
}

function Fact({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs text-fg-tertiary">{label}</dt>
      <dd className="font-semibold">{children}</dd>
    </div>
  )
}
