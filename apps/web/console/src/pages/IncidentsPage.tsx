import { Siren } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { DeclareIncidentModal, IncidentList, useIncidents } from '@/features/incidents'
import { Can, Permissions } from '@/shared/auth'
import { Button, ErrorState, PageHeader, TableSkeleton, Tabs } from '@/shared/components/ui'
import { useQueryParam } from '@/shared/hooks'

/** Serious events with an owner and a timeline. Only the Incident Manager capability can declare them. */
export function IncidentsPage() {
  const navigate = useNavigate()
  const [tab, setTab] = useQueryParam('tab', 'open')
  const [declaring, setDeclaring] = useState(false)
  const incidents = useIncidents(tab === 'open')

  return (
    <>
      <PageHeader
        title="Incidents"
        description="Every incident has an owner, and everything done about it is on its timeline."
        actions={
          <Can permission={Permissions.IncidentDeclare}>
            <Button variant="danger" icon={<Siren aria-hidden className="size-4" />} onClick={() => setDeclaring(true)}>Declare incident</Button>
          </Can>
        }
      />
      <Tabs label="Incidents" value={tab} onChange={setTab} items={[{ value: 'open', label: 'Open' }, { value: 'all', label: 'All' }]} />

      {incidents.isPending ? (
        <TableSkeleton columns={6} label="Loading incidents" />
      ) : incidents.isError ? (
        <ErrorState error={incidents.error} onRetry={() => void incidents.refetch()} />
      ) : (
        <IncidentList incidents={incidents.data} />
      )}

      {declaring && <DeclareIncidentModal open onClose={() => setDeclaring(false)} onDeclared={(id) => navigate(`/incidents/${id}`)} />}
    </>
  )
}
