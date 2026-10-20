import { useState } from 'react'
import {
  AssignModal,
  BackInServiceModal,
  CrewTable,
  NeedsList,
  OutOfServiceModal,
  RosterButton,
  RosterDutyModal,
  UnitImpactModal,
  UnitsTable,
  useAssignments,
  useCrew,
  usePlanningNeeds,
  useUnits,
  type UnitRow,
} from '@/features/planning'
import { usePeople } from '@/features/people'
import { Can, Permissions } from '@/shared/auth'
import { Button, Card, EmptyState, ErrorState, PageHeader, StatusPill, Table, TableSkeleton, Tabs, Td, Th, THead, Tr } from '@/shared/components/ui'
import { useQueryParam } from '@/shared/hooks'
import { formatDateTime, todayIso } from '@/shared/utils'

/** Every journey needs a driver and a unit. The scheduler plans and assigns; the controller can see the plan. */
export function PlanningPage() {
  const [tab, setTab] = useQueryParam('tab', 'needs')
  const [date, setDate] = useQueryParam('date', todayIso())
  const needs = usePlanningNeeds(date)
  const units = useUnits(date)
  const crew = useCrew(date)
  const assignments = useAssignments(date)
  const [assigning, setAssigning] = useState<string | null>(null)
  const [out, setOut] = useState<UnitRow | null>(null)
  const [back, setBack] = useState<UnitRow | null>(null)
  const [impact, setImpact] = useState<UnitRow | null>(null)
  const [rostering, setRostering] = useState(false)
  const drivers = usePeople('driver', rostering)

  const open = needs.data?.items.length

  return (
    <>
      <PageHeader
        title="Planning"
        description="Assign drivers and units, and see which journeys are not covered."
        actions={
          <>
            <input aria-label="Day" type="date" value={date} onChange={(event) => setDate(event.target.value)} className="h-10 rounded-control border border-line-strong bg-surface px-3" />
            <RosterButton onClick={() => setRostering(true)} />
          </>
        }
      />
      <Tabs
        label="Planning"
        value={tab}
        onChange={setTab}
        items={[
          { value: 'needs', label: 'Needs a driver or unit', count: open },
          { value: 'assignments', label: 'Assignments' },
          { value: 'units', label: 'Units' },
          { value: 'crew', label: 'Drivers' },
        ]}
      />

      {tab === 'needs' && (
        <div className="space-y-4">
          {needs.data && needs.data.unitAlerts.length > 0 && (
            <Card className="divide-y divide-line">
              {needs.data.unitAlerts.map((alert) => (
                <div key={alert.unitId} className="flex items-center justify-between gap-4 px-5 py-3.5">
                  <div>
                    <p className="font-semibold">Unit {alert.code} is out of service <StatusPill tone="cancelled">{alert.affectedJourneys} journeys</StatusPill></p>
                    <p className="text-fg-secondary">{formatDateTime(alert.from)}{alert.to && ` to ${formatDateTime(alert.to)}`} · {alert.reason}</p>
                  </div>
                  <Can permission={Permissions.JourneyVehicleAssign}>
                    <Button size="sm" onClick={() => setImpact({ unitId: alert.unitId, code: alert.code } as UnitRow)}>Find replacements</Button>
                  </Can>
                </div>
              ))}
            </Card>
          )}
          {needs.isPending ? <TableSkeleton columns={6} label="Loading planning needs" /> : needs.isError ? <ErrorState error={needs.error} onRetry={() => void needs.refetch()} /> : <NeedsList items={needs.data.items} onAssign={setAssigning} />}
        </div>
      )}

      {tab === 'assignments' &&
        (assignments.isPending ? (
          <TableSkeleton columns={4} label="Loading assignments" />
        ) : assignments.isError ? (
          <ErrorState error={assignments.error} onRetry={() => void assignments.refetch()} />
        ) : assignments.data.length === 0 ? (
          <EmptyState title="No assignments for this day" />
        ) : (
          <Table caption="Assignments by unit">
            <THead><Th>Unit</Th><Th>Drivers</Th><Th>Journeys</Th><Th>Status</Th></THead>
            <tbody>
              {assignments.data.map((row) => (
                <Tr key={row.unitId}>
                  <Td className="font-semibold">{row.code} <span className="font-normal text-fg-secondary">{row.name}</span></Td>
                  <Td>{row.drivers.join(', ') || '–'}</Td>
                  <Td className="text-fg-secondary">{row.journeyTimes.join(', ') || '–'}</Td>
                  <Td><StatusPill tone={row.status === 'Covered' ? 'ontime' : 'delayed'}>{row.status}</StatusPill></Td>
                </Tr>
              ))}
            </tbody>
          </Table>
        ))}

      {tab === 'units' &&
        (units.isPending ? <TableSkeleton columns={6} label="Loading units" /> : units.isError ? <ErrorState error={units.error} onRetry={() => void units.refetch()} /> : <UnitsTable units={units.data} onOutOfService={setOut} onBackInService={setBack} />)}

      {tab === 'crew' &&
        (crew.isPending ? <TableSkeleton columns={4} label="Loading drivers" /> : crew.isError ? <ErrorState error={crew.error} onRetry={() => void crew.refetch()} /> : <CrewTable crew={crew.data} />)}

      {assigning && <AssignModal key={assigning} journeyId={assigning} open onClose={() => setAssigning(null)} />}
      {out && <OutOfServiceModal key={out.unitId} unit={out} onClose={() => setOut(null)} />}
      {back && <BackInServiceModal key={back.unitId} unit={back} onClose={() => setBack(null)} />}
      {impact && <UnitImpactModal key={impact.unitId} unit={impact} onClose={() => setImpact(null)} />}
      {rostering && <RosterDutyModal open onClose={() => setRostering(false)} drivers={(drivers.data ?? []).map((d) => ({ subject: d.subject, name: d.name }))} />}
    </>
  )
}
