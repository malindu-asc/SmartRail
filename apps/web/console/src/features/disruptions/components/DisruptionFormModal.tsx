import { useEffect, useState } from 'react'
import { Button, Field, Modal, Select, TextArea, TextInput } from '@/shared/components/ui'
import { StationChecklist } from '@/features/network'
import { localInputToUtc, toLocalInput } from '@/shared/utils'
import { usePreviewDisruption, useSaveDisruption } from '../hooks/useDisruptions'
import { DISRUPTION_TYPES, type DisruptionBody, type DisruptionDetail, type DisruptionSeverity } from '../types'

interface Props {
  open: boolean
  onClose: () => void
  /** Editing an existing draft or live local notice. */
  existing?: DisruptionDetail
  /** Start the form from a driver's report: its text and the station it came from. The controller edits it before publishing. */
  prefill?: { title: string; message: string; stations: string[] }
  /** The scheduler announces planned works: a start time is required and the notice is not live. */
  planned?: boolean
  onSaved?: (detail: DisruptionDetail) => void
}

function initial(existing?: DisruptionDetail, planned?: boolean, prefill?: Props['prefill']) {
  return {
    title: existing?.title ?? prefill?.title ?? '',
    message: existing?.message ?? prefill?.message ?? '',
    severity: (existing?.severity ?? 'Minor') as DisruptionSeverity,
    networkWide: existing?.networkWide ?? false,
    stations: existing?.stations.map((s) => s.code) ?? prefill?.stations ?? [],
    type: existing?.type ?? (planned ? 'Planned works' : ''),
    starts: existing?.startsAtUtc ? toLocalInput(new Date(existing.startsAtUtc)) : planned ? toLocalInput(new Date(Date.now() + 24 * 3_600_000)) : '',
    // A new notice ends by itself after an hour unless the controller says otherwise: it then leaves the apps and the terminals.
    ends: existing?.expectedEndUtc ? toLocalInput(new Date(existing.expectedEndUtc)) : existing ? '' : toLocalInput(new Date(Date.now() + 3_600_000)),
    reviewBy: toLocalInput(new Date(existing?.reviewByUtc ?? Date.now() + 4 * 3_600_000)),
  }
}

type FormValues = ReturnType<typeof initial>

// Read the clock outside the component, so rendering stays pure.
const now = () => new Date()

function validate(values: FormValues, planned: boolean, at: Date) {
  return {
    title: !values.title.trim() ? 'Give the notice a title.' : null,
    message: !values.message.trim() ? 'Write the message passengers will read.' : null,
    scope: !values.networkWide && values.stations.length === 0 ? 'Choose at least one station, or make it network-wide.' : null,
    starts: planned && !values.starts ? 'Planned works need a start time.' : null,
    reviewBy: !values.reviewBy || new Date(values.reviewBy) <= at ? 'Choose a review time in the future.' : null,
  }
}

/** Write a passenger notice: what, where, how bad and when to review it. A live preview shows what passengers will see. */
export function DisruptionFormModal({ open, onClose, existing, prefill, planned = false, onSaved }: Props) {
  const [form, setForm] = useState(() => initial(existing, planned, prefill))
  const [submitted, setSubmitted] = useState(false)
  const save = useSaveDisruption({ id: existing?.id, planned })
  const preview = usePreviewDisruption()
  const { mutate: requestPreview } = preview

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) => {
    const next = { ...form, [key]: value }
    setForm(next)
    if (submitted) setErrors(validate(next, planned, now()))
  }

  // Ask for the passenger preview shortly after the person stops typing.
  useEffect(() => {
    if (!open || !form.title.trim() || !form.message.trim()) return
    const timer = setTimeout(() => requestPreview({ title: form.title, message: form.message, severity: form.severity }), 400)
    return () => clearTimeout(timer)
  }, [open, form.title, form.message, form.severity, requestPreview])

  // Errors are worked out when the person submits and again as they fix things, never while rendering.
  const [errors, setErrors] = useState(() => validate(form, planned, now()))

  function send(submit: boolean) {
    setSubmitted(true)
    const found = validate(form, planned, now())
    setErrors(found)
    if (Object.values(found).some(Boolean)) return
    const body: DisruptionBody = {
      title: form.title.trim(),
      message: form.message.trim(),
      severity: form.severity,
      networkWide: form.networkWide,
      stationCodes: form.networkWide ? [] : form.stations,
      type: form.type || undefined,
      startsAtUtc: form.starts ? localInputToUtc(form.starts) : undefined,
      expectedEndUtc: form.ends ? localInputToUtc(form.ends) : undefined,
      reviewByUtc: localInputToUtc(form.reviewBy),
      submit,
    }
    save.mutate(body, {
      onSuccess: (detail) => {
        onSaved?.(detail)
        onClose()
      },
    })
  }

  const editing = existing !== undefined
  const heading = planned ? 'Announce planned works' : editing ? 'Edit notice' : 'New disruption'

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="lg"
      title={heading}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={save.isPending}>Cancel</Button>
          <Button onClick={() => send(true)} loading={save.isPending}>{planned ? 'Announce' : 'Publish now'}</Button>
        </>
      }
    >
      <div className="space-y-4">
        <Field label="Title" error={submitted ? errors.title : null}>
          {(props) => <TextInput {...props} value={form.title} maxLength={120} onChange={(event) => set('title', event.target.value)} />}
        </Field>
        <Field label="Message to passengers" hint={`Plain text. ${form.message.length} of 1000`} error={submitted ? errors.message : null}>
          {(props) => <TextArea {...props} value={form.message} maxLength={1000} onChange={(event) => set('message', event.target.value)} />}
        </Field>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Severity">
            {(props) => (
              <Select {...props} value={form.severity} onChange={(event) => set('severity', event.target.value as DisruptionSeverity)}>
                <option value="Minor">Minor</option>
                <option value="Major">Major</option>
                <option value="Severe">Severe</option>
              </Select>
            )}
          </Field>
          <Field label="Type">
            {(props) => (
              <Select {...props} value={form.type} onChange={(event) => set('type', event.target.value)}>
                <option value="">Not set</option>
                {DISRUPTION_TYPES.map((type) => <option key={type} value={type}>{type}</option>)}
              </Select>
            )}
          </Field>
        </div>

        <div className="space-y-2">
          <p className="text-[13px] font-semibold">Where</p>
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={form.networkWide} onChange={(event) => set('networkWide', event.target.checked)} />
            Network-wide (all stations)
          </label>
          {!form.networkWide && <StationChecklist selected={form.stations} onChange={(codes) => set('stations', codes)} />}
          {submitted && errors.scope && <p role="alert" className="text-xs text-cancelled">{errors.scope}</p>}
        </div>

        <div className="grid grid-cols-3 gap-4">
          <Field label={planned ? 'Starts' : 'Starts (optional)'} error={submitted ? errors.starts : null}>
            {(props) => <TextInput {...props} type="datetime-local" value={form.starts} onChange={(event) => set('starts', event.target.value)} />}
          </Field>
          <Field label="Expected end (it disappears then)">
            {(props) => <TextInput {...props} type="datetime-local" value={form.ends} onChange={(event) => set('ends', event.target.value)} />}
          </Field>
          <Field label="Review by" error={submitted ? errors.reviewBy : null}>
            {(props) => <TextInput {...props} type="datetime-local" value={form.reviewBy} onChange={(event) => set('reviewBy', event.target.value)} />}
          </Field>
        </div>

        <div className="rounded-card bg-soft p-4" aria-live="polite">
          <p className="text-xs font-semibold uppercase tracking-wide text-fg-tertiary">What passengers will see</p>
          {preview.data ? (
            <>
              <p className="mt-2 font-semibold">{preview.data.app.title}</p>
              <p className="text-fg-secondary">{preview.data.app.detail}</p>
              <p className="mt-2 text-xs text-fg-tertiary">Platform display: {preview.data.platformDisplay}</p>
            </>
          ) : (
            <p className="mt-2 text-fg-tertiary">Write a title and a message to see the preview.</p>
          )}
        </div>
      </div>
    </Modal>
  )
}
