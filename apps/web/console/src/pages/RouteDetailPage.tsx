import { ArrowLeft } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useRoute } from '@/features/journeys'
import { AssignModal } from '@/features/planning'
import { Button, ErrorState, PageHeader, StatusPill, Table, TableSkeleton, Td, Th, THead, Tr } from '@/shared/components/ui'
import { Can, Permissions } from '@/shared/auth'
import { formatTime } from '@/shared/utils'

/** One route for the day: each journey with its driver and unit, and which ones still need assigning. */
export function RouteDetailPage() {
  const { key = '' } = useParams()
  const route = useRoute(key)
  const [assigning, setAssigning] = useState<string | null>(null)

  return (
    <>
      <Link to="/routes" className="mb-3 inline-flex items-center gap-1.5 text-fg-secondary hover:text-fg"><ArrowLeft aria-hidden className="size-4" />Routes</Link>
      <PageHeader title={route.data?.summary.name ?? 'Route'} description={route.data?.attentionText ?? undefined} />
      {route.isPending ? (
        <TableSkeleton columns={5} label="Loading the route" />
      ) : route.isError ? (
        <ErrorState error={route.error} onRetry={() => void route.refetch()} />
      ) : (
        <>
          <Table caption="Journeys on this route">
            <THead>
              <Th>Departs</Th>
              <Th>Train</Th>
              <Th>Driver</Th>
              <Th>Unit</Th>
              <Th>Assignment</Th>
              <Th><span className="sr-only">Actions</span></Th>
            </THead>
            <tbody>
              {route.data.journeys.map((journey) => (
                <Tr key={journey.journeyId} tone={journey.assignmentKind === 'Missing' ? 'attention' : undefined}>
                  <Td className="font-bold">{formatTime(journey.departsUtc)}</Td>
                  <Td><Link to={`/live-board/${journey.journeyId}`} className="font-semibold hover:underline">{journey.trainNumber}</Link></Td>
                  <Td>{journey.driverName ?? '–'}</Td>
                  <Td>{journey.unitCode ?? '–'}</Td>
                  <Td><StatusPill tone={journey.assignmentKind === 'Missing' ? 'delayed' : journey.assignmentKind === 'Warning' ? 'cancelled' : 'ontime'}>{journey.assignment}</StatusPill></Td>
                  <Td className="text-right">
                    <Can permission={[Permissions.JourneyDriverAssign, Permissions.JourneyVehicleAssign]}>
                      <Button size="sm" variant="secondary" onClick={() => setAssigning(journey.journeyId)}>Assign</Button>
                    </Can>
                  </Td>
                </Tr>
              ))}
            </tbody>
          </Table>
          {assigning && <AssignModal key={assigning} journeyId={assigning} open onClose={() => setAssigning(null)} />}
        </>
      )}
    </>
  )
}
