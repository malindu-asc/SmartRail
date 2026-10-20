import { Link } from 'react-router-dom'
import { useRoutes } from '@/features/journeys'
import { Can, Permissions } from '@/shared/auth'
import { Card, EmptyState, ErrorState, ListSkeleton, PageHeader, StatusPill, toneFor } from '@/shared/components/ui'

/** One card per route and direction, with the worst state of its journeys. "No live data" is never shown as on time. */
export function RoutesPage() {
  const routes = useRoutes()

  return (
    <>
      <PageHeader title="Routes" description="Each line and direction with the worst state of its journeys today." />
      {routes.isPending ? (
        <ListSkeleton label="Loading routes" />
      ) : routes.isError ? (
        <ErrorState error={routes.error} onRetry={() => void routes.refetch()} />
      ) : routes.data.length === 0 ? (
        <EmptyState title="No routes today" />
      ) : (
        <Card className="divide-y divide-line">
          {routes.data.map((route) => (
            <div key={route.key} className="flex items-center justify-between gap-4 px-5 py-4">
              <div>
                <p className="font-semibold">{route.name}</p>
                <p className="text-fg-secondary">
                  {route.journeysToday} journeys · {route.runningNow} running · {route.unassigned > 0 ? `${route.unassigned} without a driver or unit` : 'all assigned'}
                  {route.note && ` · ${route.note}`}
                </p>
              </div>
              <div className="flex items-center gap-3">
                <StatusPill tone={toneFor(route.statusKind)}>{route.status}</StatusPill>
                <Can permission={Permissions.PlanningRead}>
                  <Link to={`/routes/${route.key}`} className="inline-flex h-8 items-center rounded-control border border-line-strong px-3 text-[13px] font-semibold hover:bg-soft">Open</Link>
                </Can>
              </div>
            </div>
          ))}
        </Card>
      )}
    </>
  )
}
