import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { DisruptionFormModal, DisruptionsTable, useDisruptionList, type DisruptionTab } from '@/features/disruptions'
import { Permissions, useCan } from '@/shared/auth'
import { Button, ErrorState, PageHeader, TableSkeleton, Tabs } from '@/shared/components/ui'
import { useQueryParam } from '@/shared/hooks'

/**
 * Passenger notices. A controller sees Active, Awaiting approval, Drafts and Resolved and writes new ones; a scheduler sees only
 * planned works and announces new ones (REQ-OPS-016).
 */
export function DisruptionsPage() {
  const navigate = useNavigate()
  const isController = useCan(Permissions.DisruptionCreate)
  const [tab, setTab] = useQueryParam('tab', isController ? 'active' : 'planned')
  const [creating, setCreating] = useState(false)
  const list = useDisruptionList(isController ? (tab as DisruptionTab) : 'planned', !isController)
  const counts = list.data?.counts

  return (
    <>
      <PageHeader
        title={isController ? 'Disruptions' : 'Planned works'}
        description={isController ? 'Notices passengers see in the app and on displays.' : 'Announce works in advance so passengers can plan around them.'}
        actions={
          <Button icon={<Plus aria-hidden className="size-4" />} onClick={() => setCreating(true)}>
            {isController ? 'New disruption' : 'Announce planned works'}
          </Button>
        }
      />

      {isController && (
        <Tabs
          label="Disruption lists"
          value={tab}
          onChange={setTab}
          items={[
            { value: 'active', label: 'Active', count: counts?.active },
            { value: 'awaiting', label: 'Awaiting approval', count: counts?.awaitingApproval },
            { value: 'drafts', label: 'Drafts', count: counts?.drafts },
            { value: 'planned', label: 'Planned' },
            { value: 'resolved', label: 'Resolved', count: counts?.resolved },
          ]}
        />
      )}

      {list.isPending ? (
        <TableSkeleton columns={6} label="Loading disruptions" />
      ) : list.isError ? (
        <ErrorState error={list.error} onRetry={() => void list.refetch()} />
      ) : (
        <DisruptionsTable items={list.data.items} linkRows={isController} />
      )}

      {creating && <DisruptionFormModal open planned={!isController} onClose={() => setCreating(false)} onSaved={(detail) => isController && navigate(`/disruptions/${detail.id}`)} />}
    </>
  )
}
