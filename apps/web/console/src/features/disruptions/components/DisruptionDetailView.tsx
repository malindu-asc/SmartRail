import { useState } from 'react'
import { Button, Card, CardHeader, ConfirmModal, Field, StatusPill, TextArea, toneFor } from '@/shared/components/ui'
import { Can, Permissions, useCan } from '@/shared/auth'
import { formatDateTime, humanize } from '@/shared/utils'
import { useApproveDisruption, useRejectDisruption, useResolveDisruption, useRetractDisruption, useSubmitDisruption } from '../hooks/useDisruptions'
import type { DisruptionDetail } from '../types'
import { DisruptionFormModal } from './DisruptionFormModal'

type Dialog = 'edit' | 'reject' | 'retract' | 'resolve' | 'approve' | null

/** One notice with what passengers see, its history and the actions this person may take now. */
export function DisruptionDetailView({ detail }: { detail: DisruptionDetail }) {
  const [dialog, setDialog] = useState<Dialog>(null)
  const canApproveRole = useCan(Permissions.DisruptionApproveNetworkWide)
  const submit = useSubmitDisruption(detail.id)
  const close = () => setDialog(null)

  const waiting = detail.status === 'AwaitingApproval'
  const draft = detail.status === 'Draft' || detail.status === 'Rejected'
  const live = detail.status === 'Published'
  const editable = draft || (live && !detail.networkWide)

  return (
    <div className="grid gap-4 xl:grid-cols-[1.5fr_1fr]">
      <div className="space-y-4">
        <Card>
          <CardHeader title={detail.title} meta={<StatusPill tone={toneFor(detail.severity)}>{detail.severity}</StatusPill>} />
          <div className="space-y-4 px-5 py-4">
            <p className="whitespace-pre-line">{detail.message}</p>
            <dl className="grid grid-cols-2 gap-x-6 gap-y-3 text-fg-secondary">
              <Item label="Status"><StatusPill tone={toneFor(detail.status)}>{humanize(detail.status)}</StatusPill></Item>
              <Item label="Where">{detail.networkWide ? 'All stations' : detail.stations.map((s) => s.name).join(', ')}</Item>
              <Item label="Type">{detail.type ?? '–'}</Item>
              <Item label="Written by">{detail.ownerName ?? 'Unknown'}</Item>
              <Item label="Starts">{detail.startsAtUtc ? formatDateTime(detail.startsAtUtc) : 'Now'}</Item>
              <Item label="Expected end">{detail.expectedEndUtc ? formatDateTime(detail.expectedEndUtc) : 'Not set'}</Item>
              <Item label="Review by">{formatDateTime(detail.reviewByUtc)}{detail.reviewExpired && <span className="ml-2 font-semibold text-cancelled">expired</span>}</Item>
              <Item label="Journeys affected">{detail.journeysAffected ?? '–'}</Item>
            </dl>
            {detail.rejectionComment && (
              <p role="note" className="rounded-card bg-cancelled-bg p-3 text-cancelled">
                <strong>Sent back:</strong> {detail.rejectionComment}
              </p>
            )}
          </div>
        </Card>

        <Card>
          <CardHeader title="What passengers and stations see" />
          <div className="grid gap-4 px-5 py-4 md:grid-cols-2">
            <div className="rounded-card bg-soft p-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-fg-tertiary">Passenger app</p>
              <p className="mt-2 font-semibold">{detail.preview.app.title}</p>
              <p className="text-fg-secondary">{detail.preview.app.detail}</p>
            </div>
            <div className="rounded-card bg-soft p-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-fg-tertiary">Platform display</p>
              <p className="mt-2">{detail.preview.platformDisplay}</p>
              <p className="mt-2 text-xs font-semibold uppercase tracking-wide text-fg-tertiary">Station terminal</p>
              <p className="mt-1">{detail.preview.terminal}</p>
            </div>
          </div>
        </Card>
      </div>

      <div className="space-y-4">
        <Card>
          <CardHeader title="Actions" />
          <div className="flex flex-col gap-2 p-5">
            {waiting && (
              canApproveRole && detail.canApprove ? (
                <>
                  <Button onClick={() => setDialog('approve')}>Approve and publish</Button>
                  <Button variant="secondary" onClick={() => setDialog('reject')}>Send back with a comment</Button>
                </>
              ) : (
                <p className="text-fg-secondary">
                  {canApproveRole ? 'You wrote this notice, so a different controller must approve it.' : 'Waiting for a controller to approve it.'}
                </p>
              )
            )}
            {draft && (
              <Can permission={Permissions.DisruptionCreate}>
                <Button onClick={() => submit.mutate()} loading={submit.isPending}>{detail.networkWide ? 'Send for approval' : 'Publish now'}</Button>
              </Can>
            )}
            {editable && (
              <Can permission={Permissions.DisruptionCreate}>
                <Button variant="secondary" onClick={() => setDialog('edit')}>Edit notice</Button>
              </Can>
            )}
            {live && (
              <>
                <Can permission={Permissions.DisruptionPublish}>
                  <Button variant="secondary" onClick={() => setDialog('resolve')}>Mark as over</Button>
                </Can>
                <Can permission={Permissions.DisruptionRetract}>
                  <Button variant="danger" onClick={() => setDialog('retract')}>Retract, it was wrong</Button>
                </Can>
                {detail.networkWide && <p className="text-xs text-fg-tertiary">A live network-wide notice cannot be edited. Retract it and write it again.</p>}
              </>
            )}
            {!waiting && !draft && !live && <p className="text-fg-secondary">This notice is closed.</p>}
          </div>
        </Card>

        <Card>
          <CardHeader title="History" />
          <ol className="divide-y divide-line">
            {detail.history.map((entry) => (
              <li key={`${entry.at}-${entry.text}`} className="px-5 py-3">
                <p className="font-semibold">{entry.text}</p>
                <p className="text-xs text-fg-tertiary">{formatDateTime(entry.at)}{entry.person && ` · ${entry.person}`}</p>
              </li>
            ))}
          </ol>
        </Card>
      </div>

      <ApproveDialog detail={detail} open={dialog === 'approve'} onClose={close} />
      <RejectDialog detail={detail} open={dialog === 'reject'} onClose={close} />
      <RetractDialog detail={detail} open={dialog === 'retract'} onClose={close} />
      <ResolveDialog detail={detail} open={dialog === 'resolve'} onClose={close} />
      {dialog === 'edit' && <DisruptionFormModal open existing={detail} onClose={close} />}
    </div>
  )
}

function Item({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs text-fg-tertiary">{label}</dt>
      <dd className="font-semibold text-fg">{children}</dd>
    </div>
  )
}

interface DialogProps {
  detail: DisruptionDetail
  open: boolean
  onClose: () => void
}

function ApproveDialog({ detail, open, onClose }: DialogProps) {
  const approve = useApproveDisruption(detail.id)
  return (
    <ConfirmModal
      open={open}
      onClose={onClose}
      title="Approve this network-wide notice"
      description="Passengers in every station will see it straight away."
      confirmLabel="Approve and publish"
      pending={approve.isPending}
      onConfirm={() => approve.mutate(undefined, { onSuccess: onClose })}
    />
  )
}

function RejectDialog({ detail, open, onClose }: DialogProps) {
  const reject = useRejectDisruption(detail.id)
  const [comment, setComment] = useState('')
  const [submitted, setSubmitted] = useState(false)
  return (
    <ConfirmModal
      open={open}
      onClose={onClose}
      title="Send back to the author"
      description="They can fix it and send it again."
      confirmLabel="Send back"
      pending={reject.isPending}
      onConfirm={() => {
        setSubmitted(true)
        if (comment.trim()) reject.mutate(comment.trim(), { onSuccess: onClose })
      }}
    >
      <Field label="What needs to change?" error={submitted && !comment.trim() ? 'A comment is required.' : null}>
        {(props) => <TextArea {...props} value={comment} maxLength={500} onChange={(event) => setComment(event.target.value)} />}
      </Field>
    </ConfirmModal>
  )
}

function RetractDialog({ detail, open, onClose }: DialogProps) {
  const retract = useRetractDisruption(detail.id)
  const [reason, setReason] = useState('')
  const [submitted, setSubmitted] = useState(false)
  return (
    <ConfirmModal
      open={open}
      onClose={onClose}
      title="Retract this notice"
      description="It disappears from every passenger client. Use this when the notice was wrong."
      confirmLabel="Retract notice"
      tone="danger"
      pending={retract.isPending}
      onConfirm={() => {
        setSubmitted(true)
        if (reason.trim()) retract.mutate(reason.trim(), { onSuccess: onClose })
      }}
    >
      <Field label="Why was it wrong?" error={submitted && !reason.trim() ? 'Give a reason.' : null}>
        {(props) => <TextArea {...props} value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} />}
      </Field>
    </ConfirmModal>
  )
}

function ResolveDialog({ detail, open, onClose }: DialogProps) {
  const resolve = useResolveDisruption(detail.id)
  return (
    <ConfirmModal
      open={open}
      onClose={onClose}
      title="Mark as over"
      description="The notice moves to Resolved and passengers no longer see it."
      confirmLabel="Mark as over"
      pending={resolve.isPending}
      onConfirm={() => resolve.mutate(undefined, { onSuccess: onClose })}
    />
  )
}
