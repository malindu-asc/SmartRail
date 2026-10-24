import { useState } from 'react'
import { ConfirmModal, Field, TextArea } from '@/shared/components/ui'
import { useReinstate } from '../hooks/useJourneys'
import type { JourneyRow } from '../types'

interface Props {
  journey: JourneyRow
  open: boolean
  onClose: () => void
}

/** Bring a cancelled journey back, before its scheduled departure. */
export function ReinstateModal({ journey, open, onClose }: Props) {
  const reinstate = useReinstate(journey.journeyId)
  const [reason, setReason] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function confirm() {
    setSubmitted(true)
    if (!reason.trim()) return
    reinstate.mutate({ reason: reason.trim(), expectedVersion: journey.version }, { onSuccess: onClose })
  }

  return (
    <ConfirmModal
      open={open}
      onClose={onClose}
      title={`Reinstate ${journey.trainNumber}`}
      description="The journey runs again as scheduled."
      confirmLabel="Reinstate journey"
      pending={reinstate.isPending}
      onConfirm={confirm}
    >
      <Field label="Why is it back?" error={submitted && !reason.trim() ? 'Give a reason.' : null}>
        {(props) => <TextArea {...props} value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} />}
      </Field>
    </ConfirmModal>
  )
}
