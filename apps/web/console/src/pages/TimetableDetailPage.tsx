import { ArrowLeft } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { useRoutes } from '@/features/journeys'
import { VersionDetailView, useVersion } from '@/features/timetable'
import { CardSkeleton, ErrorState, PageHeader, TableSkeleton } from '@/shared/components/ui'

export function TimetableDetailPage() {
  const { id = '' } = useParams()
  const version = useVersion(id)
  const routes = useRoutes()
  const routeCodes = [...new Set((routes.data ?? []).map((r) => r.routeCode))]

  return (
    <>
      <Link to="/timetables" className="mb-3 inline-flex items-center gap-1.5 text-fg-secondary hover:text-fg"><ArrowLeft aria-hidden className="size-4" />Timetables</Link>
      <PageHeader title="Timetable version" />
      {version.isPending ? (
        <div className="space-y-4"><CardSkeleton lines={2} /><TableSkeleton columns={8} label="Loading services" /></div>
      ) : version.isError ? (
        <ErrorState error={version.error} onRetry={() => void version.refetch()} />
      ) : (
        <VersionDetailView detail={version.data} routeCodes={routeCodes} />
      )}
    </>
  )
}
