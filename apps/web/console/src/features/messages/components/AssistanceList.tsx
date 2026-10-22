import { Hand, Megaphone } from 'lucide-react'
import { useState } from 'react'
import { Button, Card, ConfirmModal, EmptyState, Field, TextInput } from '@/shared/components/ui'
import { DisruptionFormModal } from '@/features/disruptions'
import { Can, Permissions } from '@/shared/auth'
import { formatAgo, formatTime } from '@/shared/utils'
import { useHandleAssistance } from '../hooks/useMessages'
import type { Assistance } from '../types'

/**
 * Reports and help requests from drivers, oldest first. A controller marks what happened, or publishes the report to passengers: that
 * opens the disruption form filled from the driver's note, and nothing reaches the public until the controller publishes it.
 */
export function AssistanceList({ items }: { items: Assistance[] }) {
  const handle = useHandleAssistance()
  const [target, setTarget] = useState<Assistance | null>(null)
  const [outcome, setOutcome] = useState('')
  const [publishing, setPublishing] = useState<Assistance | null>(null)

  if (items.length === 0) return <EmptyState title="No reports from drivers" description="Reports and help requests from drivers appear here and on the dashboard." />

  function close() {
    setTarget(null)
    setOutcome('')
  }

  return (
    <>
      <Card className="divide-y divide-line">
        {items.map((item) => (
          <div key={item.id} className="flex items-center gap-4 px-5 py-4">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-card bg-cancelled-bg text-cancelled"><Hand aria-hidden className="size-5" /></span>
            <div className="min-w-0 flex-1">
              <p className="font-semibold">Driver report</p>
              <p className="text-fg-secondary">
                {formatTime(item.requestedAtUtc)} {item.stationName ?? item.stationCode ?? ''} · {item.driverName ?? 'Driver'}{item.trainNumber && ` · train ${item.trainNumber}`} · {formatAgo(item.requestedAtUtc)}
              </p>
              {item.note && <p className="mt-1">{item.note}</p>}
            </div>
            <Can permission={Permissions.MessageSendToDriver}>
              <div className="flex gap-2">
                <Can permission={Permissions.DisruptionCreate}>
                  <Button size="sm" variant="secondary" icon={<Megaphone aria-hidden className="size-4" />} onClick={() => setPublishing(item)}>Publish to passengers</Button>
                </Can>
                <Button size="sm" onClick={() => setTarget(item)}>Mark handled</Button>
              </div>
            </Can>
          </div>
        ))}
      </Card>

      {publishing && (
        <DisruptionFormModal
          open
          prefill={{
            title: publishing.note?.slice(0, 120) ?? 'Service update',
            message: publishing.note ?? '',
            stations: publishing.stationCode ? [publishing.stationCode] : [],
          }}
          onClose={() => setPublishing(null)}
        />
      )}

      <ConfirmModal
        open={target !== null}
        onClose={close}
        title="Mark as handled"
        description="Say what was done, so the next controller knows."
        confirmLabel="Mark handled"
        pending={handle.isPending}
        onConfirm={() => target && handle.mutate({ id: target.id, outcome: outcome.trim() || undefined }, { onSuccess: close })}
      >
        <Field label="Outcome (optional)">{(props) => <TextInput {...props} value={outcome} maxLength={300} onChange={(event) => setOutcome(event.target.value)} />}</Field>
      </ConfirmModal>
    </>
  )
}
