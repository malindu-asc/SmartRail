import { Hand } from 'lucide-react'
import { useState } from 'react'
import { Button, Card, ConfirmModal, EmptyState, Field, TextInput } from '@/shared/components/ui'
import { Can, Permissions } from '@/shared/auth'
import { formatAgo, formatTime } from '@/shared/utils'
import { useHandleAssistance } from '../hooks/useMessages'
import type { Assistance } from '../types'

/** Drivers asking for help, oldest first. Someone has to take it and mark what happened. */
export function AssistanceList({ items }: { items: Assistance[] }) {
  const handle = useHandleAssistance()
  const [target, setTarget] = useState<Assistance | null>(null)
  const [outcome, setOutcome] = useState('')

  if (items.length === 0) return <EmptyState title="No one is asking for help" description="Requests from drivers appear here and on the dashboard." />

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
              <p className="font-semibold">Assistance requested</p>
              <p className="text-fg-secondary">
                {formatTime(item.requestedAtUtc)} {item.stationName ?? item.stationCode ?? ''} · {item.driverName ?? 'Driver'}{item.trainNumber && ` · train ${item.trainNumber}`} · {formatAgo(item.requestedAtUtc)}
              </p>
              {item.note && <p className="mt-1">{item.note}</p>}
            </div>
            <Can permission={Permissions.MessageSendToDriver}>
              <Button size="sm" onClick={() => setTarget(item)}>Mark handled</Button>
            </Can>
          </div>
        ))}
      </Card>

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
