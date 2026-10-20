import { ArrowLeft } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { DisruptionDetailView, useDisruption } from '@/features/disruptions'
import { CardSkeleton, ErrorState, PageHeader } from '@/shared/components/ui'

/** One notice with its preview, history and the actions this person may take (approve, reject, retract ...). */
export function DisruptionDetailPage() {
  const { id = '' } = useParams()
  const disruption = useDisruption(id)

  return (
    <>
      <Link to="/disruptions" className="mb-3 inline-flex items-center gap-1.5 text-fg-secondary hover:text-fg"><ArrowLeft aria-hidden className="size-4" />Disruptions</Link>
      <PageHeader title="Disruption" />
      {disruption.isPending ? (
        <div className="grid gap-4 xl:grid-cols-[1.5fr_1fr]"><CardSkeleton lines={6} /><CardSkeleton lines={4} /></div>
      ) : disruption.isError ? (
        <ErrorState error={disruption.error} onRetry={() => void disruption.refetch()} />
      ) : (
        <DisruptionDetailView detail={disruption.data} />
      )}
    </>
  )
}
