import { AuditTable, useAudit, type AuditFilters } from '@/features/audit'
import { Permissions, useCan } from '@/shared/auth'
import { ErrorState, FilterBar, PageHeader, Select, TableSkeleton, TextInput } from '@/shared/components/ui'
import { useQueryParam } from '@/shared/hooks'

/**
 * Who did what, to what, and why. Append-only. A controller reads the operational and the access log; an administrator reads the
 * access log only (and is sent to it).
 */
export function AuditPage({ accessOnly = false }: { accessOnly?: boolean }) {
  const canOperational = useCan(Permissions.AuditReadOperational)
  const forced = accessOnly || !canOperational
  const [kind, setKind] = useQueryParam('kind', forced ? 'access' : 'operational')
  const [query, setQuery] = useQueryParam('q')
  const [from, setFrom] = useQueryParam('from')
  const [to, setTo] = useQueryParam('to')

  const filters: AuditFilters = { kind: forced ? 'access' : (kind as 'operational' | 'access'), q: query, from: from || undefined, to: to || undefined }
  const audit = useAudit(filters)

  return (
    <>
      <PageHeader title="Audit log" description={forced ? 'Changes to people, roles, devices and security settings.' : 'Actions on journeys, notices, incidents, messages and timetables, and changes to access.'} />
      <FilterBar>
        {!forced && (
          <div className="w-52">
            <Select pill aria-label="Which log" value={kind} onChange={(event) => setKind(event.target.value)}>
              <option value="operational">Operations</option>
              <option value="access">Access changes</option>
            </Select>
          </div>
        )}
        <TextInput aria-label="Search" type="search" placeholder="Search action, person or reason" className="w-72 rounded-full" value={query} onChange={(event) => setQuery(event.target.value)} />
        <label className="flex items-center gap-2 font-semibold">From <TextInput type="date" className="w-44" value={from} onChange={(event) => setFrom(event.target.value)} /></label>
        <label className="flex items-center gap-2 font-semibold">To <TextInput type="date" className="w-44" value={to} onChange={(event) => setTo(event.target.value)} /></label>
      </FilterBar>

      {audit.isPending ? (
        <TableSkeleton columns={6} label="Loading the audit log" />
      ) : audit.isError ? (
        <ErrorState error={audit.error} onRetry={() => void audit.refetch()} />
      ) : (
        <AuditTable entries={audit.data} />
      )}
    </>
  )
}

/** The administrator's route: always the access log. */
export function AdminAuditPage() {
  return <AuditPage accessOnly />
}
