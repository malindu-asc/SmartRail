import { Link as LinkIcon, Send } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Button, Card, CardHeader, Field, StatusPill, TextArea, toneFor } from '@/shared/components/ui'
import { Can, Permissions } from '@/shared/auth'
import { formatDateTime } from '@/shared/utils'
import { useAddIncidentNote } from '../hooks/useIncidents'
import type { IncidentDetail } from '../types'
import { HandOverModal, LinkDisruptionModal, ResolveIncidentModal } from './IncidentModals'

const KIND_TONE = { Note: 'neutral', Action: 'brand', Handover: 'changed', System: 'stale' } as const

/** One incident: who owns it, what has been done (the timeline) and what it has told people. */
export function IncidentDetailView({ detail }: { detail: IncidentDetail }) {
  const { incident } = detail
  const [modal, setModal] = useState<'handover' | 'resolve' | 'link' | null>(null)
  const open = incident.status === 'Open'

  return (
    <div className="grid gap-4 xl:grid-cols-[1.6fr_1fr]">
      <Card>
        <CardHeader title="Timeline" />
        <ol className="divide-y divide-line">
          {[...detail.timeline].reverse().map((entry) => (
            <li key={`${entry.at}-${entry.text}`} className="flex gap-4 px-5 py-3.5">
              <span className="w-14 shrink-0 font-semibold text-fg-secondary">{formatDateTime(entry.at).split(', ')[1] ?? formatDateTime(entry.at)}</span>
              <div className="min-w-0">
                <p className="flex flex-wrap items-center gap-2 font-semibold">
                  {entry.text}
                  <StatusPill tone={KIND_TONE[entry.kind]}>{entry.kind}</StatusPill>
                </p>
                {entry.detail && <p className="text-fg-secondary">{entry.detail}</p>}
                <p className="text-xs text-fg-tertiary">{entry.authorName ?? entry.author}</p>
              </div>
            </li>
          ))}
        </ol>
        {open && (
          <Can permission={Permissions.IncidentDeclare}>
            <AddNote incidentId={incident.id} />
          </Can>
        )}
      </Card>

      <div className="space-y-4">
        <Card>
          <CardHeader title="Incident" meta={<StatusPill tone={toneFor(incident.severity)}>{incident.severity}</StatusPill>} />
          <dl className="space-y-3 px-5 py-4">
            <Row label="Status" value={<StatusPill tone={open ? 'delayed' : 'ontime'}>{incident.status}</StatusPill>} />
            <Row label="Owner" value={incident.ownerName ?? 'Unknown'} />
            <Row label="Opened" value={formatDateTime(incident.openedAtUtc)} />
            {detail.reportedBy && <Row label="Reported by" value={detail.reportedBy} />}
            {detail.resolutionNote && <Row label="Outcome" value={detail.resolutionNote} />}
          </dl>
          {open && (
            <div className="flex flex-wrap gap-2 border-t border-line px-5 py-4">
              <Can permission={Permissions.IncidentReassignOwner}>
                <Button variant="secondary" icon={<Send aria-hidden className="size-4" />} onClick={() => setModal('handover')}>Hand over</Button>
              </Can>
              <Can permission={Permissions.IncidentDeclare}>
                <Button variant="secondary" icon={<LinkIcon aria-hidden className="size-4" />} onClick={() => setModal('link')}>Link notice</Button>
                <Button onClick={() => setModal('resolve')}>Resolve</Button>
              </Can>
            </div>
          )}
        </Card>

        <Card>
          <CardHeader title="Who has been told" />
          <dl className="space-y-3 px-5 py-4">
            <Row label="Passenger alerts" value={detail.told.passengerAlerts === null ? 'Not available yet' : detail.told.passengerAlerts} />
            <Row label="Station displays" value={detail.told.stationDisplays} />
            <Row label="Driver messages" value={`${detail.told.driverMessagesAcknowledged} of ${detail.told.driverMessages} acknowledged`} />
            <Row
              label="Passenger notice"
              value={detail.linked.disruptionId ? <Link to={`/disruptions/${detail.linked.disruptionId}`} className="font-semibold text-primary hover:underline">{detail.linked.disruptionTitle}</Link> : 'None linked'}
            />
          </dl>
        </Card>
      </div>

      <HandOverModal incident={incident} open={modal === 'handover'} onClose={() => setModal(null)} />
      <ResolveIncidentModal incident={incident} open={modal === 'resolve'} onClose={() => setModal(null)} />
      <LinkDisruptionModal incident={incident} open={modal === 'link'} onClose={() => setModal(null)} />
    </div>
  )
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-fg-secondary">{label}</dt>
      <dd className="text-right font-semibold">{value}</dd>
    </div>
  )
}

function AddNote({ incidentId }: { incidentId: string }) {
  const add = useAddIncidentNote(incidentId)
  const [text, setText] = useState('')
  const [kind, setKind] = useState<'Note' | 'Action'>('Note')

  return (
    <form
      className="space-y-3 border-t border-line px-5 py-4"
      onSubmit={(event) => {
        event.preventDefault()
        if (!text.trim()) return
        add.mutate({ kind, text: text.trim() }, { onSuccess: () => setText('') })
      }}
    >
      <Field label="Add to the timeline">
        {(props) => <TextArea {...props} value={text} maxLength={300} placeholder="What happened or what you did" onChange={(event) => setText(event.target.value)} />}
      </Field>
      <div className="flex items-center justify-between">
        <label className="flex items-center gap-2 text-fg-secondary">
          <input type="checkbox" checked={kind === 'Action'} onChange={(event) => setKind(event.target.checked ? 'Action' : 'Note')} />
          This is an action I took
        </label>
        <Button type="submit" loading={add.isPending} disabled={!text.trim()}>Add</Button>
      </div>
    </form>
  )
}
