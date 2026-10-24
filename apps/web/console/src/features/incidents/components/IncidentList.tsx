import { Link } from 'react-router-dom'
import { EmptyState, StatusPill, Table, Td, Th, THead, Tr, toneFor } from '@/shared/components/ui'
import { formatDateTime } from '@/shared/utils'
import { useStationNames } from '@/features/network'
import type { Incident } from '../types'

export function IncidentList({ incidents }: { incidents: Incident[] }) {
  const stationName = useStationNames()
  if (incidents.length === 0) return <EmptyState title="No incidents" description="Declared incidents appear here with their owner and timeline." />

  return (
    <Table caption="Incidents">
      <THead>
        <Th>Opened</Th>
        <Th>Incident</Th>
        <Th>Severity</Th>
        <Th>Where</Th>
        <Th>Owner</Th>
        <Th>Status</Th>
      </THead>
      <tbody>
        {incidents.map((incident) => (
          <Tr key={incident.id} tone={incident.severity === 'Critical' && incident.status === 'Open' ? 'danger' : undefined}>
            <Td className="whitespace-nowrap">{formatDateTime(incident.openedAtUtc)}</Td>
            <Td><Link to={`/incidents/${incident.id}`} className="font-semibold hover:underline">{incident.title}</Link></Td>
            <Td><StatusPill tone={toneFor(incident.severity)}>{incident.severity}</StatusPill></Td>
            <Td className="text-fg-secondary">{incident.stationCodes.map(stationName).join(', ') || '–'}</Td>
            <Td>{incident.ownerName ?? 'Unknown'}</Td>
            <Td><StatusPill tone={incident.status === 'Open' ? 'delayed' : 'ontime'}>{incident.status}</StatusPill></Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
