import { useState } from 'react'
import { Can, Permissions } from '@/shared/auth'
import { Button, EmptyState, ErrorState, StatusPill, Table, TableSkeleton, Td, Th, THead, Tr } from '@/shared/components/ui'
import { formatTime } from '@/shared/utils'
import { useApprovePlan, useGeneratePlan, usePlanningWeek } from '../hooks/usePlanning'

const dayName = new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' })

/** The week ahead: pick a day, see every journey with its driver and unit, and assign the ones that need it. */
export function WeekPlan({ onAssign }: { onAssign: (journeyId: string) => void }) {
  const week = usePlanningWeek()
  const generate = useGeneratePlan()
  const approve = useApprovePlan()
  const [selected, setSelected] = useState(0)

  if (week.isPending) return <TableSkeleton columns={6} label="Loading the week" />
  if (week.isError) return <ErrorState error={week.error} onRetry={() => void week.refetch()} />

  const day = week.data.days[Math.min(selected, week.data.days.length - 1)]
  const allApproved = week.data.days.every((d) => d.approved)
  if (!day) return <EmptyState title="No days to show" />

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-fg-secondary">
          {allApproved ? 'The plan for this week is approved. Drivers can see their duties.' : 'Generate the plan, change what needs changing, then approve it. Drivers see their duties only after approval.'}
        </p>
        <Can permission={Permissions.JourneyDriverAssign}>
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => generate.mutate()} loading={generate.isPending}>Generate plan</Button>
            <Button onClick={() => approve.mutate()} loading={approve.isPending} disabled={allApproved}>Approve plan</Button>
          </div>
        </Can>
      </div>
      <div role="tablist" aria-label="Day of the week" className="grid grid-cols-7 gap-2">
        {week.data.days.map((d, index) => (
          <button
            key={d.date}
            type="button"
            role="tab"
            aria-selected={index === selected}
            onClick={() => setSelected(index)}
            className={`rounded-card border px-3 py-2 text-left ${index === selected ? 'border-primary bg-soft' : 'border-line bg-surface hover:bg-soft'}`}
          >
            <span className="block font-semibold">{dayName.format(new Date(`${d.date}T00:00:00Z`))}</span>
            <span className="block text-fg-secondary">{d.journeys} journeys</span>
            <span className={`block font-semibold ${d.unassigned > 0 ? 'text-delayed' : 'text-on-time'}`}>
              {d.unassigned > 0 ? `${d.unassigned} need a driver or unit` : 'All covered'}
            </span>
            <span className="block text-fg-secondary">{d.approved ? 'Approved' : 'Draft'}</span>
          </button>
        ))}
      </div>

      {day.rows.length === 0 ? (
        <EmptyState title="No journeys on this day" />
      ) : (
        <Table caption={`Journeys on ${dayName.format(new Date(`${day.date}T00:00:00Z`))}`}>
          <THead>
            <Th>Departs</Th>
            <Th>Train</Th>
            <Th>Route</Th>
            <Th>Driver</Th>
            <Th>Unit</Th>
            <Th>Assignment</Th>
            <Th><span className="sr-only">Actions</span></Th>
          </THead>
          <tbody>
            {day.rows.map((row) => (
              <Tr key={row.journeyId} tone={row.assignment.startsWith('Needs') ? 'attention' : undefined}>
                <Td className="font-bold">{formatTime(row.departsUtc)}</Td>
                <Td className="font-semibold">{row.trainNumber}</Td>
                <Td className="text-fg-secondary">{row.route}</Td>
                <Td>{row.driverName ?? '–'}</Td>
                <Td>{row.unitCode ?? '–'}</Td>
                <Td><StatusPill tone={row.assignment.startsWith('Needs') ? 'delayed' : row.assignmentKind === 'Good' ? 'ontime' : 'cancelled'}>{row.assignment}</StatusPill></Td>
                <Td className="text-right">
                  <Can permission={[Permissions.JourneyDriverAssign, Permissions.JourneyVehicleAssign]}>
                    <Button size="sm" variant="secondary" onClick={() => onAssign(row.journeyId)}>Assign</Button>
                  </Can>
                </Td>
              </Tr>
            ))}
          </tbody>
        </Table>
      )}
    </div>
  )
}
