import { Button, EmptyState, StatusPill, Table, Td, Th, THead, Tr } from '@/shared/components/ui'
import { Can, Permissions } from '@/shared/auth'
import { formatTime } from '@/shared/utils'
import type { PlanningNeed } from '../types'

interface Props {
  items: PlanningNeed[]
  onAssign: (journeyId: string) => void
}

/** Journeys that still need a driver or a unit, soonest first, with the system's suggestion. */
export function NeedsList({ items, onAssign }: Props) {
  if (items.length === 0) return <EmptyState title="Every journey has a driver and a unit" description="New gaps appear here as soon as they happen." />

  return (
    <Table caption="Journeys that need a driver or a unit">
      <THead>
        <Th>Departs</Th>
        <Th>Train</Th>
        <Th>Route</Th>
        <Th>Missing</Th>
        <Th>Suggestion</Th>
        <Th><span className="sr-only">Actions</span></Th>
      </THead>
      <tbody>
        {items.map((need) => (
          <Tr key={need.journeyId} tone={need.departsInMinutes < 120 ? 'attention' : undefined}>
            <Td className="font-bold">{formatTime(need.departsUtc)} <span className="font-normal text-fg-tertiary">in {need.departsInMinutes < 90 ? `${need.departsInMinutes} min` : `${Math.round(need.departsInMinutes / 60)} h`}</span></Td>
            <Td>{need.trainNumber}</Td>
            <Td>{need.route}</Td>
            <Td><StatusPill tone="delayed">{need.missing}</StatusPill></Td>
            <Td className="text-fg-secondary">
              {[need.suggestedDriver?.driverName, need.suggestedUnit?.code].filter(Boolean).join(' · ') || need.detail || '–'}
            </Td>
            <Td className="text-right">
              <Can permission={[Permissions.JourneyDriverAssign, Permissions.JourneyVehicleAssign]}>
                <Button size="sm" onClick={() => onAssign(need.journeyId)}>Assign</Button>
              </Can>
            </Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
