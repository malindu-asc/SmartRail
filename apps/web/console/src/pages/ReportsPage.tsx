import { Download } from 'lucide-react'
import { useState } from 'react'
import { PunctualityDays, PunctualityLines, PunctualitySummary, fetchPunctualityCsv, usePunctuality } from '@/features/reports'
import { Button, CardSkeleton, ErrorState, FilterBar, PageHeader, StatCardsSkeleton, TableSkeleton, TextInput, notify } from '@/shared/components/ui'
import { useQueryParam } from '@/shared/hooks'
import { todayIso } from '@/shared/utils'

function daysAgo(days: number): string {
  return todayIso(new Date(Date.now() - days * 24 * 3_600_000))
}

/** Punctuality for a period: on time, average delay and cancellations by day and route, with a CSV download. */
export function ReportsPage() {
  const [from, setFrom] = useQueryParam('from', daysAgo(6))
  const [to, setTo] = useQueryParam('to', todayIso())
  const [downloading, setDownloading] = useState(false)
  const filters = { from, to }
  const report = usePunctuality(filters)

  async function download() {
    setDownloading(true)
    try {
      const csv = await fetchPunctualityCsv(filters)
      const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }))
      const link = document.createElement('a')
      link.href = url
      link.download = `punctuality-${from}-to-${to}.csv`
      link.click()
      URL.revokeObjectURL(url)
      notify.success('Report downloaded.')
    } catch (error) {
      notify.error(error, 'Could not download the report.')
    } finally {
      setDownloading(false)
    }
  }

  return (
    <>
      <PageHeader title="Punctuality report" description="No live data is never counted as on time." actions={<Button variant="secondary" icon={<Download aria-hidden className="size-4" />} onClick={() => void download()} loading={downloading}>Download CSV</Button>} />
      <FilterBar>
        <label className="flex items-center gap-2 font-semibold">From <TextInput type="date" className="w-44" value={from} max={to} onChange={(event) => setFrom(event.target.value)} /></label>
        <label className="flex items-center gap-2 font-semibold">To <TextInput type="date" className="w-44" value={to} min={from} onChange={(event) => setTo(event.target.value)} /></label>
      </FilterBar>

      {report.isPending ? (
        <div className="space-y-4"><StatCardsSkeleton count={4} /><CardSkeleton lines={6} /><TableSkeleton columns={5} rows={4} /></div>
      ) : report.isError ? (
        <ErrorState error={report.error} onRetry={() => void report.refetch()} />
      ) : (
        <div className="space-y-4">
          <PunctualitySummary report={report.data} />
          <PunctualityDays days={report.data.days} />
          <PunctualityLines lines={report.data.lines} />
        </div>
      )}
    </>
  )
}
