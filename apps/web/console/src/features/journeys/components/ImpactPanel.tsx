import { Users } from 'lucide-react'
import { ErrorState, Skeleton } from '@/shared/components/ui'
import { useImpact } from '../hooks/useJourneys'
import type { ImpactAction } from '../types'

interface ImpactPanelProps {
  journeyId: string
  action: ImpactAction
  stopSequence?: number
  /** Only fetch while the modal is open. */
  open: boolean
}

/** "Who is affected" before a risky action is confirmed (tickets, displays, the driver). */
export function ImpactPanel({ journeyId, action, stopSequence, open }: ImpactPanelProps) {
  const impact = useImpact(journeyId, action, stopSequence, open)

  if (impact.isPending) {
    return (
      <div role="status" aria-label="Checking who is affected" className="space-y-2 rounded-card bg-soft p-4">
        <Skeleton className="h-4 w-40" />
        <Skeleton className="h-3 w-56" />
        <Skeleton className="h-3 w-48" />
      </div>
    )
  }
  if (impact.isError) return <ErrorState error={impact.error} onRetry={() => void impact.refetch()} />

  const data = impact.data
  return (
    <div className="rounded-card bg-soft p-4">
      <p className="flex items-center gap-2 font-semibold text-fg">
        <Users aria-hidden className="size-4" />
        This will affect
      </p>
      <ul className="mt-2 space-y-1 text-fg-secondary">
        {data.lines.map((line) => (
          <li key={line}>{line}</li>
        ))}
        {data.tickets > 0 && (
          <li>
            {data.tickets} {data.tickets === 1 ? 'ticket' : 'tickets'}, {data.travellers} {data.travellers === 1 ? 'traveller' : 'travellers'}
          </li>
        )}
        <li>{data.followers === null ? 'People following this train: not available yet' : `${data.followers} people following this train`}</li>
      </ul>
    </div>
  )
}
