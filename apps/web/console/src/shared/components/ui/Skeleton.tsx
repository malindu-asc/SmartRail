import { cn } from '@/shared/utils'
import { Card } from './Card'

/** A grey shimmering block. Skeletons take the shape of the content that is loading, so the page does not jump. */
export function Skeleton({ className }: { className?: string }) {
  return <div aria-hidden className={cn('sr-skeleton rounded-control', className)} />
}

/** Wraps a skeleton so screen readers hear "Loading" once. */
export function LoadingRegion({ label, children, className }: { label: string; children: React.ReactNode; className?: string }) {
  return (
    <div role="status" aria-live="polite" aria-busy="true" className={className}>
      <span className="sr-only">{label}</span>
      {children}
    </div>
  )
}

/** Table placeholder: a header row and rows of cells. */
export function TableSkeleton({ rows = 6, columns = 6, label = 'Loading' }: { rows?: number; columns?: number; label?: string }) {
  return (
    <LoadingRegion label={label}>
      <Card className="overflow-hidden">
        <div className="grid gap-4 border-b border-line bg-soft px-4 py-3" style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}>
          {Array.from({ length: columns }, (_, i) => (
            <Skeleton key={i} className="h-3 w-16" />
          ))}
        </div>
        {Array.from({ length: rows }, (_, row) => (
          <div key={row} className="grid items-center gap-4 border-b border-line px-4 py-4 last:border-0" style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}>
            {Array.from({ length: columns }, (_, col) => (
              <Skeleton key={col} className={cn('h-4', col === 0 ? 'w-12' : col % 2 ? 'w-24' : 'w-3/4')} />
            ))}
          </div>
        ))}
      </Card>
    </LoadingRegion>
  )
}

/** Row of stat cards. */
export function StatCardsSkeleton({ count = 6 }: { count?: number }) {
  return (
    <LoadingRegion label="Loading numbers" className="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-6">
      {Array.from({ length: count }, (_, i) => (
        <Card key={i} className="space-y-3 p-5">
          <Skeleton className="h-3 w-16" />
          <Skeleton className="h-8 w-14" />
          <Skeleton className="h-3 w-24" />
        </Card>
      ))}
    </LoadingRegion>
  )
}

/** A card with a few text lines. */
export function CardSkeleton({ lines = 4, label = 'Loading' }: { lines?: number; label?: string }) {
  return (
    <LoadingRegion label={label}>
      <Card className="space-y-4 p-5">
        <Skeleton className="h-5 w-40" />
        {Array.from({ length: lines }, (_, i) => (
          <Skeleton key={i} className={cn('h-4', i % 2 ? 'w-2/3' : 'w-full')} />
        ))}
      </Card>
    </LoadingRegion>
  )
}

/** A list of two-line rows (needs attention, messages). */
export function ListSkeleton({ rows = 5, label = 'Loading' }: { rows?: number; label?: string }) {
  return (
    <LoadingRegion label={label}>
      <Card className="divide-y divide-line">
        {Array.from({ length: rows }, (_, i) => (
          <div key={i} className="flex items-center gap-4 px-5 py-4">
            <Skeleton className="size-10 rounded-card" />
            <div className="flex-1 space-y-2">
              <Skeleton className="h-4 w-1/3" />
              <Skeleton className="h-3 w-1/2" />
            </div>
            <Skeleton className="h-8 w-16" />
          </div>
        ))}
      </Card>
    </LoadingRegion>
  )
}
