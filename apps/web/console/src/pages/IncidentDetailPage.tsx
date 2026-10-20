import { ArrowLeft } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { IncidentDetailView, useIncident } from '@/features/incidents'
import { CardSkeleton, ErrorState, PageHeader } from '@/shared/components/ui'

export function IncidentDetailPage() {
  const { id = '' } = useParams()
  const incident = useIncident(id)

  return (
    <>
      <Link to="/incidents" className="mb-3 inline-flex items-center gap-1.5 text-fg-secondary hover:text-fg"><ArrowLeft aria-hidden className="size-4" />Incidents</Link>
      <PageHeader title={incident.data?.incident.title ?? 'Incident'} />
      {incident.isPending ? (
        <div className="grid gap-4 xl:grid-cols-[1.6fr_1fr]"><CardSkeleton lines={7} /><CardSkeleton lines={4} /></div>
      ) : incident.isError ? (
        <ErrorState error={incident.error} onRetry={() => void incident.refetch()} />
      ) : (
        <IncidentDetailView detail={incident.data} />
      )}
    </>
  )
}
