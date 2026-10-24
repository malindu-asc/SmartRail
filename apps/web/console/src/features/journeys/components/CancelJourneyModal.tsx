import { useState } from 'react'
import { Field, Modal, TextArea, Button } from '@/shared/components/ui'
import { useCancelJourney, useReasonCodes } from '../hooks/useJourneys'
import type { JourneyRow } from '../types'
import { ImpactPanel } from './ImpactPanel'
import { ReasonFields } from './ReasonFields'

interface Props {
  journey: JourneyRow
  open: boolean
  onClose: () => void
}

/** Cancel a journey that has not left. Needs a reason; the passenger message is optional. */
export function CancelJourneyModal({ journey, open, onClose }: Props) {
  const reasons = useReasonCodes()
  const cancel = useCancelJourney(journey.journeyId)
  const [reasonCode, setReasonCode] = useState('')
  const [note, setNote] = useState('')
  const [message, setMessage] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function submit() {
    setSubmitted(true)
    if (!reasonCode) return
    cancel.mutate(
      { reasonCode, note: note || undefined, passengerMessage: message || undefined, expectedVersion: journey.version },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={`Cancel ${journey.trainNumber}, ${journey.originName} to ${journey.destinationName}`}
      description="The journey is cancelled for everyone. Tickets on it are marked Affected."
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={cancel.isPending}>Keep the journey</Button>
          <Button variant="danger" onClick={submit} loading={cancel.isPending}>Cancel journey</Button>
        </>
      }
    >
      <div className="space-y-4">
        <ImpactPanel journeyId={journey.journeyId} action="cancel" open={open} />
        <ReasonFields
          reasons={reasons.data?.cancel ?? []}
          reasonCode={reasonCode}
          note={note}
          onReasonCode={setReasonCode}
          onNote={setNote}
          error={submitted && !reasonCode ? 'Choose a reason.' : null}
        />
        <Field label="Message to passengers (optional)" hint="Plain text. Shown in the app next to the cancellation.">
          {(props) => <TextArea {...props} value={message} maxLength={200} onChange={(event) => setMessage(event.target.value)} />}
        </Field>
      </div>
    </Modal>
  )
}
