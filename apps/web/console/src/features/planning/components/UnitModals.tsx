import { useState } from 'react'
import { Button, ConfirmModal, ErrorState, Field, Modal, Skeleton, TextArea, TextInput, StatusPill } from '@/shared/components/ui'
import { formatTime, localInputToUtc, toLocalInput } from '@/shared/utils'
import { useApplySuggestions, useBackInService, useMarkOutOfService, useUnitImpact } from '../hooks/usePlanning'
import type { UnitRow } from '../types'

interface UnitModalProps {
  unit: UnitRow | null
  onClose: () => void
}

/** Mark a unit out of service for a period. The server answers with the journeys that lose it. */
export function OutOfServiceModal({ unit, onClose }: UnitModalProps) {
  const mark = useMarkOutOfService(unit?.unitId ?? '')
  const [from, setFrom] = useState(() => toLocalInput(new Date()))
  const [to, setTo] = useState(() => toLocalInput(new Date(Date.now() + 8 * 3_600_000)))
  const [reason, setReason] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const invalid = new Date(to) <= new Date(from)

  function submit() {
    setSubmitted(true)
    if (!reason.trim() || invalid) return
    mark.mutate({ fromUtc: localInputToUtc(from), toUtc: localInputToUtc(to), reason: reason.trim() }, { onSuccess: onClose })
  }

  return (
    <Modal
      open={unit !== null}
      onClose={onClose}
      title={`Take ${unit?.code ?? 'unit'} out of service`}
      description="Journeys that use it will need another unit. You will see them straight after."
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mark.isPending}>Cancel</Button>
          <Button onClick={submit} loading={mark.isPending}>Mark out of service</Button>
        </>
      }
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Field label="From">{(props) => <TextInput {...props} type="datetime-local" value={from} onChange={(event) => setFrom(event.target.value)} />}</Field>
          <Field label="Until" error={submitted && invalid ? 'Must be after the start.' : null}>
            {(props) => <TextInput {...props} type="datetime-local" value={to} onChange={(event) => setTo(event.target.value)} />}
          </Field>
        </div>
        <Field label="Reason" error={submitted && !reason.trim() ? 'Give a reason.' : null}>
          {(props) => <TextArea {...props} value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} />}
        </Field>
      </div>
    </Modal>
  )
}

/** Confirm that a unit is back, with the effect on its journeys. */
export function BackInServiceModal({ unit, onClose }: UnitModalProps) {
  const back = useBackInService(unit?.unitId ?? '')
  const impact = useUnitImpact(unit?.unitId ?? '', unit !== null)
  const periodId = impact.data?.periodId

  return (
    <ConfirmModal
      open={unit !== null}
      onClose={onClose}
      title={`${unit?.code ?? 'Unit'} is back in service`}
      description="It can be assigned to journeys again."
      confirmLabel="Back in service"
      pending={back.isPending}
      confirmDisabled={!periodId}
      onConfirm={() => periodId && back.mutate(periodId, { onSuccess: onClose })}
    >
      {impact.isPending && <Skeleton className="h-4 w-48" />}
    </ConfirmModal>
  )
}

/** Journeys that lost a unit and the replacement the system suggests for each; apply them all or leave them. */
export function UnitImpactModal({ unit, onClose }: UnitModalProps) {
  const impact = useUnitImpact(unit?.unitId ?? '', unit !== null)
  const apply = useApplySuggestions(unit?.unitId ?? '')

  return (
    <Modal
      open={unit !== null}
      onClose={onClose}
      size="lg"
      title={`${unit?.code ?? 'Unit'}: journeys that need another unit`}
      description={impact.data?.reason ?? undefined}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Close</Button>
          <Button
            loading={apply.isPending}
            disabled={!impact.data || impact.data.journeys.every((j) => !j.suggestedUnit)}
            onClick={() => apply.mutate({ reason: 'Unit out of service' }, { onSuccess: onClose })}
          >
            Apply all suggestions
          </Button>
        </>
      }
    >
      {impact.isPending ? (
        <div role="status" aria-label="Loading journeys" className="space-y-3"><Skeleton className="h-10 w-full" /><Skeleton className="h-10 w-full" /></div>
      ) : impact.isError ? (
        <ErrorState error={impact.error} onRetry={() => void impact.refetch()} />
      ) : impact.data.journeys.length === 0 ? (
        <p className="text-fg-secondary">No journeys lose this unit.</p>
      ) : (
        <ul className="divide-y divide-line">
          {impact.data.journeys.map((journey) => (
            <li key={journey.journeyId} className="flex items-center justify-between gap-4 py-3">
              <div>
                <p className="font-semibold">{journey.trainNumber} <span className="font-normal text-fg-secondary">{journey.route}, {formatTime(journey.departsUtc)}</span></p>
                <p className="text-fg-secondary">{journey.currentUnit ? `Was on ${journey.currentUnit}` : 'No unit'}</p>
              </div>
              {journey.suggestedUnit ? <StatusPill tone="ontime">Suggested {journey.suggestedUnit.code}</StatusPill> : <StatusPill tone="cancelled">No unit free</StatusPill>}
            </li>
          ))}
        </ul>
      )}
    </Modal>
  )
}
