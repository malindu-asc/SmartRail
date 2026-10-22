import { useState } from 'react'
import { Button, Card, CardHeader, ConfirmModal, EmptyState, StatusPill, Table, Td, Th, THead, Tr } from '@/shared/components/ui'
import { Can, Permissions, useCan } from '@/shared/auth'
import { formatDate } from '@/shared/utils'
import { useComparison, useRemoveService } from '../hooks/useTimetable'
import type { VersionDetail, VersionService } from '../types'
import { AddServiceModal, ApproveVersionModal, EditTimesModal, SubmitVersionModal } from './TimetableModals'
import { VersionStatusPill } from './VersionsTable'

interface Props {
  detail: VersionDetail
  routeCodes: string[]
}

/** One timetable version: its services, what changed against the version in force, and the workflow buttons. */
export function VersionDetailView({ detail, routeCodes }: Props) {
  const { version, services, actions } = detail
  const canDraft = useCan(Permissions.TimetableDraft)
  const [dialog, setDialog] = useState<'add' | 'submit' | 'approve' | null>(null)
  const [editing, setEditing] = useState<VersionService | null>(null)
  const [removing, setRemoving] = useState<VersionService | null>(null)
  const remove = useRemoveService(version.id)
  const comparison = useComparison(version.id, version.status !== 'Superseded' && !version.inForce)
  const calendarCodes = [...new Set(services.map((s) => s.calendarCode))].sort()
  const editable = actions.canEdit && canDraft

  return (
    <div className="space-y-4">
      <Card>
        <div className="flex flex-wrap items-center justify-between gap-4 px-5 py-4">
          <div className="space-y-1">
            <p className="flex items-center gap-3 text-lg font-bold">{version.label} <VersionStatusPill version={version} /></p>
            <p className="text-fg-secondary">
              {version.services} services · written by {version.authorName ?? 'Unknown'}
              {version.effectiveFrom && ` · takes effect ${formatDate(version.effectiveFrom)}`}
              {version.approvedByName && ` · approved by ${version.approvedByName}`}
            </p>
          </div>
          <div className="flex items-center gap-3">
            {actions.canSubmit && <Can permission={Permissions.TimetableDraft}><Button onClick={() => setDialog('submit')}>Send for review</Button></Can>}
            {version.status === 'InReview' && (
              <Can permission={Permissions.TimetableApprove}>
                {actions.canApprove ? (
                  <Button onClick={() => setDialog('approve')}>Approve</Button>
                ) : (
                  <span className="text-fg-secondary">{actions.approveBlockedReason ?? 'Waiting for approval.'}</span>
                )}
              </Can>
            )}
          </div>
        </div>
      </Card>

      {comparison.data && (
        <Card>
          <CardHeader title={`Compared with ${comparison.data.againstLabel}`} meta={
            <span className="flex gap-2">
              <StatusPill tone="ontime">{comparison.data.added} added</StatusPill>
              <StatusPill tone="cancelled">{comparison.data.removed} removed</StatusPill>
              <StatusPill tone="delayed">{comparison.data.changed} changed</StatusPill>
            </span>
          } />
          {comparison.data.changes.length === 0 ? (
            <p className="px-5 py-4 text-fg-secondary">No differences yet.</p>
          ) : (
            <ul className="divide-y divide-line">
              {comparison.data.changes.map((change) => (
                <li key={`${change.kind}-${change.trainNumber}`} className="flex items-start gap-3 px-5 py-3">
                  <StatusPill tone={change.kind === 'Added' ? 'ontime' : change.kind === 'Removed' ? 'cancelled' : 'delayed'}>{change.kind}</StatusPill>
                  <p><strong>{change.trainNumber}</strong> <span className="text-fg-secondary">{change.summary}</span></p>
                </li>
              ))}
            </ul>
          )}
        </Card>
      )}

      <div className="flex items-center justify-between">
        <h2 className="text-base font-semibold">Services</h2>
        {editable && <Button variant="secondary" onClick={() => setDialog('add')}>Add a service</Button>}
      </div>

      {services.length === 0 ? (
        <EmptyState title="No services in this version" description="Add a service to the draft." />
      ) : (
        <Table caption="Services in this version">
          <THead>
            <Th>Train</Th>
            <Th>Route</Th>
            <Th>From</Th>
            <Th>To</Th>
            <Th>Departs</Th>
            <Th>Arrives</Th>
            <Th>Runs</Th>
            <Th><span className="sr-only">Actions</span></Th>
          </THead>
          <tbody>
            {services.map((service) => (
              <Tr key={service.id}>
                <Td className="font-bold">{service.trainNumber}</Td>
                <Td>{service.routeCode} <span className="text-xs text-fg-tertiary">{service.direction}</span></Td>
                <Td>{service.origin}</Td>
                <Td>{service.destination}</Td>
                <Td>{service.firstDeparture ?? '–'}</Td>
                <Td>{service.lastArrival ?? '–'}</Td>
                <Td className="text-fg-secondary">{service.calendarCode}</Td>
                <Td className="whitespace-nowrap text-right">
                  {editable && (
                    <>
                      <Button size="sm" variant="ghost" onClick={() => setEditing(service)}>Change times</Button>
                      <Button size="sm" variant="ghost" onClick={() => setRemoving(service)}>Remove</Button>
                    </>
                  )}
                </Td>
              </Tr>
            ))}
          </tbody>
        </Table>
      )}

      <AddServiceModal versionId={version.id} open={dialog === 'add'} onClose={() => setDialog(null)} routeCodes={routeCodes} calendarCodes={calendarCodes} />
      <SubmitVersionModal versionId={version.id} label={version.label} open={dialog === 'submit'} onClose={() => setDialog(null)} />
      <ApproveVersionModal versionId={version.id} label={version.label} open={dialog === 'approve'} onClose={() => setDialog(null)} />
      <EditTimesModal key={editing?.id} versionId={version.id} service={editing} onClose={() => setEditing(null)} />
      <ConfirmModal
        open={removing !== null}
        onClose={() => setRemoving(null)}
        title={`Remove ${removing?.trainNumber ?? 'service'}`}
        description="It is removed from this draft only. The timetable in force does not change."
        confirmLabel="Remove service"
        tone="danger"
        pending={remove.isPending}
        onConfirm={() => removing && remove.mutate(removing.id, { onSuccess: () => setRemoving(null) })}
      />
    </div>
  )
}
