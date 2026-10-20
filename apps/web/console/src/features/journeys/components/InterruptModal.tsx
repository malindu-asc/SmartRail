import { useState } from 'react'
import { Button, Modal } from '@/shared/components/ui'
import { useInterrupt, useReasonCodes } from '../hooks/useJourneys'
import type { JourneyRow } from '../types'
import { ReasonFields } from './ReasonFields'

interface Props {
  journey: JourneyRow
  open: boolean
  onClose: () => void
}

/** Stop the live estimate while a running train is held. Resume brings it back. */
export function InterruptModal({ journey, open, onClose }: Props) {
  const reasons = useReasonCodes()
  const interrupt = useInterrupt(journey.journeyId)
  const [reasonCode, setReasonCode] = useState('')
  const [note, setNote] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function submit() {
    setSubmitted(true)
    if (!reasonCode) return
    interrupt.mutate({ reasonCode, note: note || undefined, expectedVersion: journey.version }, { onSuccess: onClose })
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={`Interrupt ${journey.trainNumber}`}
      description="While interrupted, passengers see no estimate for this train."
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={interrupt.isPending}>Keep running</Button>
          <Button onClick={submit} loading={interrupt.isPending}>Interrupt journey</Button>
        </>
      }
    >
      <ReasonFields
        reasons={reasons.data?.interrupt ?? []}
        reasonCode={reasonCode}
        note={note}
        onReasonCode={setReasonCode}
        onNote={setNote}
        error={submitted && !reasonCode ? 'Choose a reason.' : null}
      />
    </Modal>
  )
}
