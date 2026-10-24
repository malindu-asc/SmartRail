import { DashboardGrid, useDashboard } from '@/features/dashboard'
import { CardSkeleton, ErrorState, ListSkeleton, PageHeader, StatCardsSkeleton } from '@/shared/components/ui'

/** The controller's home: the numbers, what needs attention, devices and active disruptions. */
export function DashboardPage() {
  const dashboard = useDashboard()

  return (
    <>
      <PageHeader title="Network dashboard" description="What is running, what is wrong, and what needs you." />
      {dashboard.isPending ? (
        <div className="space-y-4">
          <StatCardsSkeleton />
          <div className="grid gap-4 xl:grid-cols-[1.4fr_1fr]">
            <ListSkeleton label="Loading what needs attention" />
            <CardSkeleton lines={5} />
          </div>
        </div>
      ) : dashboard.isError ? (
        <ErrorState error={dashboard.error} onRetry={() => void dashboard.refetch()} />
      ) : (
        <DashboardGrid data={dashboard.data} />
      )}
    </>
  )
}
