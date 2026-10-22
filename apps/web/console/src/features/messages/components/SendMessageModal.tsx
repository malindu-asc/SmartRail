import { useState } from 'react'
import { Button, Field, Modal, Select, TextArea } from '@/shared/components/ui'
import { useMessageTemplates, useSendMessage } from '../hooks/useMessages'

interface Props {
  open: boolean
  onClose: () => void
  /** Fixed recipient: the driver of this journey. When absent the person picks a driver. */
  journeyId?: string
  incidentId?: string
  drivers?: { subject: string; name: string }[]
}

const VALID_FOR = [
  { minutes: 15, label: '15 minutes' },
  { minutes: 60, label: '1 hour' },
  { minutes: 240, label: '4 hours' },
]

/** A short plain-text message to a driver, optionally needing an acknowledgement. */
export function SendMessageModal({ open, onClose, journeyId, incidentId, drivers = [] }: Props) {
  const templates = useMessageTemplates()
  const send = useSendMessage()
  const [driver, setDriver] = useState('')
  const [template, setTemplate] = useState('')
  const [body, setBody] = useState('')
  const [validFor, setValidFor] = useState(60)
  const [needsAck, setNeedsAck] = useState(true)
  const [submitted, setSubmitted] = useState(false)

  const recipientMissing = !journeyId && !driver
  const bodyMissing = !body.trim()

  function pickTemplate(code: string) {
    setTemplate(code)
    const found = templates.data?.find((t) => t.code === code)
    if (found) setBody(found.body)
  }

  function submit() {
    setSubmitted(true)
    if (recipientMissing || bodyMissing) return
    send.mutate(
      {
        driverSubject: journeyId ? undefined : driver,
        journeyId,
        incidentId,
        template: template || undefined,
        body: body.trim(),
        requiresAck: needsAck,
        validUntilUtc: new Date(Date.now() + validFor * 60_000).toISOString(),
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Message a driver"
      description="Plain text. The driver sees it in the app; you will see when they have read and acknowledged it."
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={send.isPending}>Cancel</Button>
          <Button onClick={submit} loading={send.isPending}>Send message</Button>
        </>
      }
    >
      <div className="space-y-4">
        {!journeyId && (
          <Field label="Driver" error={submitted && recipientMissing ? 'Choose a driver.' : null}>
            {(props) => (
              <Select {...props} value={driver} onChange={(event) => setDriver(event.target.value)}>
                <option value="">Choose a driver</option>
                {drivers.map((d) => <option key={d.subject} value={d.subject}>{d.name}</option>)}
              </Select>
            )}
          </Field>
        )}
        <Field label="Start from a template (optional)">
          {(props) => (
            <Select {...props} value={template} onChange={(event) => pickTemplate(event.target.value)}>
              <option value="">Write my own</option>
              {(templates.data ?? []).map((t) => <option key={t.code} value={t.code}>{t.label}</option>)}
            </Select>
          )}
        </Field>
        <Field label="Message" hint={`${body.length} of 500`} error={submitted && bodyMissing ? 'Write a message.' : null}>
          {(props) => <TextArea {...props} value={body} maxLength={500} onChange={(event) => setBody(event.target.value)} />}
        </Field>
        <div className="grid grid-cols-2 items-end gap-4">
          <Field label="Valid for">
            {(props) => (
              <Select {...props} value={validFor} onChange={(event) => setValidFor(Number(event.target.value))}>
                {VALID_FOR.map((v) => <option key={v.minutes} value={v.minutes}>{v.label}</option>)}
              </Select>
            )}
          </Field>
          <label className="flex h-10 items-center gap-2 font-semibold">
            <input type="checkbox" checked={needsAck} onChange={(event) => setNeedsAck(event.target.checked)} />
            Needs acknowledgement
          </label>
        </div>
      </div>
    </Modal>
  )
}
