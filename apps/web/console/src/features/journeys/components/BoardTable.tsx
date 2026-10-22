import { EmptyState, Table, Td, Th, THead, Tr } from '@/shared/components/ui'
import { formatDelay, formatTime } from '@/shared/utils'
import type { JourneyRow } from '../types'
import { DataCell, JourneyStatus, PlatformCell } from './JourneyStatus'

interface BoardTableProps {
  journeys: JourneyRow[]
  onOpen: (journeyId: string) => void
}

/** The live board. Rows that need attention are shaded; a row opens the journey. */
export function BoardTable({ journeys, onOpen }: BoardTableProps) {
  if (journeys.length === 0) {
    return <EmptyState title="No journeys match" description="Change the filters, or pick another day." />
  }

  return (
    <Table caption="Live board">
      <THead>
        <Th>Time</Th>
        <Th>Journey</Th>
        <Th>Next stop</Th>
        <Th>Platform</Th>
        <Th>Delay</Th>
        <Th>Status</Th>
        <Th>Data</Th>
        <Th>Driver</Th>
        <Th>Unit</Th>
      </THead>
      <tbody>
        {journeys.map((journey) => (
          <Tr key={journey.journeyId} tone={journey.needsAttention ? 'attention' : undefined} onClick={() => onOpen(journey.journeyId)}>
            <Td className="font-bold">
              <button type="button" className="font-bold hover:underline" onClick={(event) => { event.stopPropagation(); onOpen(journey.journeyId) }}>
                {formatTime(journey.scheduledDepartureUtc)}
              </button>
            </Td>
            <Td>
              {journey.originName} to {journey.destinationName}
              <span className="ml-2 text-xs text-fg-tertiary">{journey.trainNumber}</span>
            </Td>
            <Td>{journey.nextStopName ?? <span className="text-fg-tertiary">–</span>}</Td>
            <Td><PlatformCell journey={journey} /></Td>
            <Td className={journey.delayMinutes && journey.delayMinutes > 1 ? 'font-semibold text-delayed' : 'font-semibold text-on-time'}>
              {journey.delayMinutes === null ? <span className="text-fg-tertiary">–</span> : formatDelay(journey.delayMinutes)}
            </Td>
            <Td><JourneyStatus journey={journey} /></Td>
            <Td><DataCell journey={journey} /></Td>
            <Td>{journey.driverName ?? <span className="text-fg-tertiary">–</span>}</Td>
            <Td>{journey.unitCode ?? <span className="text-fg-tertiary">–</span>}</Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
