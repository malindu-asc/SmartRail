import { useState } from 'react'
import { Button, Field, Modal, Select } from '@/shared/components/ui'
import { useReasonCodes, useTerminateShort } from '../hooks/useJourneys'
import type { JourneyDetail } from '../types'
import { ImpactPanel } from './ImpactPanel'
import { ReasonFields } from './ReasonFields'

interface Props {
  detail: JourneyDetail
  open: boolean
  onClose: () => void
}

/** End a running journey at a stop it has not passed; the stops after it are cancelled. */
export function TerminateShortModal({ detail, open, onClose }: Props) {
  const { journey, stops } = detail
  const reasons = useReasonCodes()
  const terminate = useTerminateShort(journey.journeyId)
  const options = stops.filter((stop) => stop.state !== 'Cancelled' && !stop.actualDepartureUtc && stop.sequence < Math.max(...stops.map((s) => s.sequence)))
  const [stop, setStop] = useState('')
  const [reasonCode, setReasonCode] = useState('')
  const [note, setNote] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function submit() {
    setSubmitted(true)
    if (!stop || !reasonCode) return
    terminate.mutate({ stopSequence: Number(stop), reasonCode, note: note || undefined, expectedVersion: journey.version }, { onSuccess: onClose })
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={`Terminate ${journey.trainNumber} short`}
      description="The train ends its run at the stop you choose. Later stops are cancelled."
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={terminate.isPending}>Keep the journey</Button>
          <Button variant="danger" onClick={submit} loading={terminate.isPending}>Terminate short</Button>
        </>
      }
    >
      <div className="space-y-4">
        <Field label="Ends at" error={submitted && !stop ? 'Choose where the train ends.' : null}>
          {(props) => (
            <Select {...props} value={stop} onChange={(event) => setStop(event.target.value)}>
              <option value="">Choose a stop</option>
              {options.map((option) => (
                <option key={option.sequence} value={option.sequence}>
                  {option.stationName}
                </option>
              ))}
            </Select>
          )}
        </Field>
        {stop && <ImpactPanel journeyId={journey.journeyId} action="terminate_short" stopSequence={Number(stop)} open={open} />}
        <ReasonFields
          reasons={reasons.data?.terminateShort ?? []}
          reasonCode={reasonCode}
          note={note}
          onReasonCode={setReasonCode}
          onNote={setNote}
          error={submitted && !reasonCode ? 'Choose a reason.' : null}
        />
      </div>
    </Modal>
  )
}
