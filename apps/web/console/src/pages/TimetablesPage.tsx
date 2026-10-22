import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { CreateDraftModal, VersionsTable, useVersions } from '@/features/timetable'
import { Can, Permissions } from '@/shared/auth'
import { Button, ErrorState, PageHeader, TableSkeleton } from '@/shared/components/ui'

/** Timetable versions: the one in force, drafts and history. A new version is drafted, sent for review and approved. */
export function TimetablesPage() {
  const navigate = useNavigate()
  const versions = useVersions()
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader
        title="Timetables"
        description="Published versions never change. To change a timetable, start a draft, send it for review and approve it."
        actions={
          <Can permission={Permissions.TimetableDraft}>
            <Button icon={<Plus aria-hidden className="size-4" />} onClick={() => setCreating(true)}>New draft</Button>
          </Can>
        }
      />
      {versions.isPending ? (
        <TableSkeleton columns={6} label="Loading timetable versions" />
      ) : versions.isError ? (
        <ErrorState error={versions.error} onRetry={() => void versions.refetch()} />
      ) : (
        <VersionsTable versions={versions.data} />
      )}
      {creating && <CreateDraftModal open onClose={() => setCreating(false)} onCreated={(id) => navigate(`/timetables/${id}`)} />}
    </>
  )
}
