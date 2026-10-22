import { useNavigate } from 'react-router-dom'
import { BoardTable, useBoard, useRoutes, type BoardFilters } from '@/features/journeys'
import { StationSelect } from '@/features/network'
import { ErrorState, FilterBar, PageHeader, Select, StatusPill, TableSkeleton, TextInput } from '@/shared/components/ui'
import { useQueryParam } from '@/shared/hooks'
import { todayIso } from '@/shared/utils'

const STATUSES = [
  { value: '', label: 'Any status' },
  { value: 'OnTime', label: 'On time' },
  { value: 'Delayed', label: 'Delayed' },
  { value: 'Cancelled', label: 'Cancelled' },
  { value: 'NoLiveData', label: 'No live data' },
  { value: 'NotStarted', label: 'Not started' },
]

/** Today's journeys with state, delay, data freshness, driver and unit. Filters live in the URL so a view can be shared. */
export function LiveBoardPage() {
  const navigate = useNavigate()
  const [route, setRoute] = useQueryParam('route')
  const [station, setStation] = useQueryParam('station')
  const [status, setStatus] = useQueryParam('status')
  const [date, setDate] = useQueryParam('date', todayIso())
  const [query, setQuery] = useQueryParam('q')
  const [attention, setAttention] = useQueryParam('attention')

  const filters: BoardFilters = { date, route, station, status, q: query, attention: attention === '1' || undefined }
  const board = useBoard(filters)
  const routes = useRoutes()
  const routeCodes = [...new Set((routes.data ?? []).map((r) => r.routeCode))]

  return (
    <>
      <PageHeader title="Live board" description="Open a journey to act on it. Rows that need attention are shaded." />
      <FilterBar
        trailing={
          board.data && (
            <>
              <span className="text-fg-secondary">{board.data.total} journeys</span>
              {board.data.needAttention > 0 && <StatusPill tone="delayed">{board.data.needAttention} need attention</StatusPill>}
            </>
          )
        }
      >
        <div className="w-40">
          <Select pill aria-label="Line" value={route} onChange={(event) => setRoute(event.target.value)}>
            <option value="">All lines</option>
            {routeCodes.map((code) => <option key={code} value={code}>{code}</option>)}
          </Select>
        </div>
        <div className="w-48"><StationSelect pill aria-label="Station" value={station} onChange={setStation} placeholder="All stations" /></div>
        <div className="w-44">
          <Select pill aria-label="Status" value={status} onChange={(event) => setStatus(event.target.value)}>
            {STATUSES.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
          </Select>
        </div>
        <TextInput aria-label="Day" type="date" className="w-44 rounded-full" value={date} onChange={(event) => setDate(event.target.value)} />
        <TextInput aria-label="Search" type="search" placeholder="Search train or station" className="w-56 rounded-full" value={query} onChange={(event) => setQuery(event.target.value)} />
        <label className="flex items-center gap-2 font-semibold">
          <input type="checkbox" checked={attention === '1'} onChange={(event) => setAttention(event.target.checked ? '1' : '')} />
          Needs attention
        </label>
      </FilterBar>

      {board.isPending ? (
        <TableSkeleton columns={9} label="Loading the live board" />
      ) : board.isError ? (
        <ErrorState error={board.error} onRetry={() => void board.refetch()} />
      ) : (
        <>
          <BoardTable journeys={board.data.items} onOpen={(id) => navigate(`/live-board/${id}`)} />
          <p className="mt-4 text-xs text-fg-tertiary">Data: Live = train feed within 60 s · Stale = 60 s to 5 min · Unknown = no update for 5 min.</p>
        </>
      )}
    </>
  )
}
