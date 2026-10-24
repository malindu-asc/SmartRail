import { Button, EmptyState, StatusPill, Table, Td, Th, THead, Tr } from '@/shared/components/ui'
import { Can, Permissions } from '@/shared/auth'
import { formatDateTime } from '@/shared/utils'
import type { UnitRow } from '../types'

interface Props {
  units: UnitRow[]
  onOutOfService: (unit: UnitRow) => void
  onBackInService: (unit: UnitRow) => void
}

/** Units with their status. Marking a unit out of service is the Fleet Controller capability (scheduler). */
export function UnitsTable({ units, onOutOfService, onBackInService }: Props) {
  if (units.length === 0) return <EmptyState title="No units" description="Units appear here when the fleet is set up." />

  return (
    <Table caption="Units">
      <THead>
        <Th>Unit</Th>
        <Th>Kind</Th>
        <Th>Status</Th>
        <Th>Journeys today</Th>
        <Th>Reason</Th>
        <Th><span className="sr-only">Actions</span></Th>
      </THead>
      <tbody>
        {units.map((unit) => {
          const out = unit.status === 'OutOfService'
          return (
            <Tr key={unit.unitId} tone={out ? 'danger' : undefined}>
              <Td className="font-semibold">{unit.code} <span className="font-normal text-fg-secondary">{unit.name}</span></Td>
              <Td>{unit.kind}</Td>
              <Td>
                <StatusPill tone={out ? 'cancelled' : 'ontime'}>{out ? 'Out of service' : 'Available'}</StatusPill>
                {out && unit.outOfServiceFromUtc && <span className="ml-2 text-xs text-fg-secondary">{formatDateTime(unit.outOfServiceFromUtc)} to {formatDateTime(unit.outOfServiceToUtc)}</span>}
              </Td>
              <Td>{unit.journeysToday}</Td>
              <Td className="text-fg-secondary">{unit.reason ?? '–'}</Td>
              <Td className="text-right">
                <Can permission={Permissions.VehicleAvailabilitySet}>
                  {out ? (
                    <Button size="sm" variant="secondary" onClick={() => onBackInService(unit)}>Back in service</Button>
                  ) : (
                    <Button size="sm" variant="secondary" onClick={() => onOutOfService(unit)}>Out of service</Button>
                  )}
                </Can>
              </Td>
            </Tr>
          )
        })}
      </tbody>
    </Table>
  )
}
