import { useState } from 'react'
import { Button, Field, Modal, Select, TextInput } from '@/shared/components/ui'
import { useChangePlatform, usePlatformCheck, useReasonCodes } from '../hooks/useJourneys'
import type { JourneyDetail } from '../types'
import { ReasonFields } from './ReasonFields'
import { formatTime } from '@/shared/utils'

interface Props {
  detail: JourneyDetail
  open: boolean
  onClose: () => void
}

/**
 * Change the platform at one stop. The platform is checked first: if another train would share it, the controller sees which
 * and must confirm the overlap (REQ-OPS-004).
 */
export function ChangePlatformModal({ detail, open, onClose }: Props) {
  const { journey, stops } = detail
  const reasons = useReasonCodes()
  const change = useChangePlatform(journey.journeyId)
  const options = stops.filter((stop) => stop.state !== 'Cancelled' && stop.kind !== 'Passing')
  const [stop, setStop] = useState(String(journey.nextStopCode ? (stops.find((s) => s.stationCode === journey.nextStopCode)?.sequence ?? options[0]?.sequence ?? '') : (options[0]?.sequence ?? '')))
  const [platform, setPlatform] = useState('')
  const [reasonCode, setReasonCode] = useState('')
  const [note, setNote] = useState('')
  const [confirmOverlap, setConfirmOverlap] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  const check = usePlatformCheck(journey.journeyId, Number(stop), platform.trim(), open && stop !== '')
  const conflicts = check.data?.conflicts ?? []
  const blocked = conflicts.length > 0 && !confirmOverlap

  function submit() {
    setSubmitted(true)
    if (!stop || !platform.trim() || !reasonCode || blocked) return
    change.mutate(
      { stopSequence: Number(stop), platform: platform.trim(), reasonCode, note: note || undefined, confirmOverlap, expectedVersion: journey.version },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={`Change platform for ${journey.trainNumber}`}
      description="Displays and the passenger app show the new platform, and the old one as 'was'."
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={change.isPending}>Keep the platform</Button>
          <Button onClick={submit} loading={change.isPending} disabled={blocked}>Change platform</Button>
        </>
      }
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Field label="At stop">
            {(props) => (
              <Select {...props} value={stop} onChange={(event) => { setStop(event.target.value); setConfirmOverlap(false) }}>
                {options.map((option) => (
                  <option key={option.sequence} value={option.sequence}>
                    {option.stationName}{option.platform ? ` (now ${option.platform})` : ''}
                  </option>
                ))}
              </Select>
            )}
          </Field>
          <Field label="New platform" error={submitted && !platform.trim() ? 'Enter a platform.' : null}>
            {(props) => <TextInput {...props} value={platform} maxLength={4} onChange={(event) => { setPlatform(event.target.value); setConfirmOverlap(false) }} />}
          </Field>
        </div>

        {check.data && check.data.available && <p className="text-on-time">Platform {check.data.platform} is free.</p>}
        {conflicts.length > 0 && (
          <div role="alert" className="rounded-card bg-delayed-bg p-4 text-delayed">
            <p className="font-semibold">Platform {platform.trim()} is in use around that time</p>
            <ul className="mt-1 list-disc pl-5">
              {conflicts.map((conflict) => (
                <li key={conflict.journeyId}>
                  Train {conflict.trainNumber}, {formatTime(conflict.from)} to {formatTime(conflict.to)}
                </li>
              ))}
            </ul>
            <label className="mt-3 flex items-center gap-2 font-semibold">
              <input type="checkbox" checked={confirmOverlap} onChange={(event) => setConfirmOverlap(event.target.checked)} />
              I understand and want to share the platform
            </label>
          </div>
        )}

        <ReasonFields
          reasons={reasons.data?.platform ?? []}
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
