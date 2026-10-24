import { StatusPill, Table, Td, Th, THead, Tr } from '@/shared/components/ui'
import { formatTime } from '@/shared/utils'
import type { StopView } from '../types'

function when(stop: StopView, which: 'arrival' | 'departure'): string {
  const actual = which === 'arrival' ? stop.actualArrivalUtc : stop.actualDepartureUtc
  const expected = which === 'arrival' ? stop.expectedArrivalUtc : stop.expectedDepartureUtc
  const scheduled = which === 'arrival' ? stop.scheduledArrivalUtc : stop.scheduledDepartureUtc
  return formatTime(actual ?? expected ?? scheduled)
}

/** Every stop with its scheduled, actual or expected time, platform and where the last update came from. */
export function StopsTable({ stops }: { stops: StopView[] }) {
  return (
    <Table caption="Stops">
      <THead>
        <Th>#</Th>
        <Th>Stop</Th>
        <Th>Arrives</Th>
        <Th>Departs</Th>
        <Th>Platform</Th>
        <Th>State</Th>
        <Th>Source</Th>
      </THead>
      <tbody>
        {stops.map((stop) => (
          <Tr key={stop.sequence} tone={stop.state === 'Cancelled' ? 'danger' : undefined}>
            <Td className="text-fg-tertiary">{stop.sequence}</Td>
            <Td className="font-semibold">{stop.stationName}</Td>
            <Td>{stop.scheduledArrivalUtc || stop.actualArrivalUtc ? when(stop, 'arrival') : '–'}</Td>
            <Td>{stop.scheduledDepartureUtc || stop.actualDepartureUtc ? when(stop, 'departure') : '–'}</Td>
            <Td>
              {stop.platform ?? '–'}
              {stop.originalPlatform && stop.originalPlatform !== stop.platform && <span className="ml-1 text-xs text-changed">was {stop.originalPlatform}</span>}
            </Td>
            <Td><StatusPill tone={stop.state === 'Cancelled' ? 'cancelled' : 'neutral'}>{stop.state}</StatusPill></Td>
            <Td className="text-fg-secondary">{stop.source}</Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
