import { useState } from 'react'
import { Button, ConfirmModal, ErrorState, Field, Modal, Select, Skeleton, StatusPill, TextInput } from '@/shared/components/ui'
import { StationSelect } from '@/features/network'
import { parseClock, todayIso } from '@/shared/utils'
import {
  useAddService,
  useApproveVersion,
  useChangeTimes,
  useComparison,
  useCreateDraft,
  useSubmitVersion,
} from '../hooks/useTimetable'
import type { StopTimeInput, VersionService } from '../types'

/** Start a draft as a copy of the timetable in force. */
export function CreateDraftModal({ open, onClose, onCreated }: { open: boolean; onClose: () => void; onCreated: (id: string) => void }) {
  const create = useCreateDraft()
  const [label, setLabel] = useState('')
  const [submitted, setSubmitted] = useState(false)

  return (
    <ConfirmModal
      open={open}
      onClose={onClose}
      title="New timetable draft"
      description="The draft starts as a copy of the timetable in force. Nothing changes for passengers until another scheduler approves it."
      confirmLabel="Create draft"
      pending={create.isPending}
      onConfirm={() => {
        setSubmitted(true)
        if (label.trim()) create.mutate(label.trim(), { onSuccess: (detail) => onCreated(detail.version.id) })
      }}
    >
      <Field label="Name" hint="For example: Winter 2026" error={submitted && !label.trim() ? 'Give the draft a name.' : null}>
        {(props) => <TextInput {...props} value={label} maxLength={100} onChange={(event) => setLabel(event.target.value)} />}
      </Field>
    </ConfirmModal>
  )
}

/** Change the times of one service; the stops stay as they are. Times are HH:mm, hours may pass 24. */
export function EditTimesModal({ versionId, service, onClose }: { versionId: string; service: VersionService | null; onClose: () => void }) {
  const change = useChangeTimes(versionId)
  const [times, setTimes] = useState<Record<number, { arrival: string; departure: string }>>({})
  const [submitted, setSubmitted] = useState(false)

  const stops = service?.stops ?? []
  const value = (sequence: number, which: 'arrival' | 'departure', fallback: string | null) => times[sequence]?.[which] ?? fallback ?? ''
  const invalid = (text: string) => text !== '' && parseClock(text) === null

  function setTime(sequence: number, which: 'arrival' | 'departure', text: string, stop: { arrival: string | null; departure: string | null }) {
    setTimes((current) => ({
      ...current,
      [sequence]: { arrival: current[sequence]?.arrival ?? stop.arrival ?? '', departure: current[sequence]?.departure ?? stop.departure ?? '', [which]: text },
    }))
  }

  function submit() {
    if (!service) return
    setSubmitted(true)
    const stopsOut: StopTimeInput[] = stops.map((stop) => ({
      stationCode: stop.stationCode,
      arrival: value(stop.sequence, 'arrival', stop.arrival) || null,
      departure: value(stop.sequence, 'departure', stop.departure) || null,
    }))
    if (stopsOut.some((s) => invalid(s.arrival ?? '') || invalid(s.departure ?? ''))) return
    change.mutate({ serviceId: service.id, stops: stopsOut }, { onSuccess: onClose })
  }

  return (
    <Modal
      open={service !== null}
      onClose={onClose}
      size="lg"
      title={`Change times for ${service?.trainNumber ?? ''}`}
      description="The first stop has only a departure, the last only an arrival. Times cannot go backwards."
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={change.isPending}>Cancel</Button>
          <Button onClick={submit} loading={change.isPending}>Save times</Button>
        </>
      }
    >
      <table className="w-full">
        <thead>
          <tr className="text-left text-[13px] text-fg-secondary"><th className="py-2">Stop</th><th>Arrives</th><th>Departs</th></tr>
        </thead>
        <tbody>
          {stops.map((stop, index) => {
            const first = index === 0
            const last = index === stops.length - 1
            const arrival = value(stop.sequence, 'arrival', stop.arrival)
            const departure = value(stop.sequence, 'departure', stop.departure)
            return (
              <tr key={stop.sequence} className="border-t border-line">
                <td className="py-2 pr-3 font-semibold">{stop.stationName}</td>
                <td className="py-1.5 pr-3">
                  <TextInput aria-label={`${stop.stationName} arrival`} className="h-9 w-24" disabled={first} value={arrival} placeholder="HH:mm" aria-invalid={submitted && invalid(arrival) ? true : undefined} onChange={(event) => setTime(stop.sequence, 'arrival', event.target.value, stop)} />
                </td>
                <td className="py-1.5">
                  <TextInput aria-label={`${stop.stationName} departure`} className="h-9 w-24" disabled={last} value={departure} placeholder="HH:mm" aria-invalid={submitted && invalid(departure) ? true : undefined} onChange={(event) => setTime(stop.sequence, 'departure', event.target.value, stop)} />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
      {submitted && <p role="alert" className="mt-2 text-xs text-fg-tertiary">Use HH:mm, for example 07:45.</p>}
    </Modal>
  )
}

interface AddServiceProps {
  versionId: string
  open: boolean
  onClose: () => void
  routeCodes: string[]
  calendarCodes: string[]
}

interface StopRow {
  stationCode: string
  arrival: string
  departure: string
}

/** Add a new service to a draft: its train number, route, days, direction and a time at each stop. */
export function AddServiceModal({ versionId, open, onClose, routeCodes, calendarCodes }: AddServiceProps) {
  const add = useAddService(versionId)
  const [trainNumber, setTrainNumber] = useState('')
  const [routeCode, setRouteCode] = useState('')
  const [calendarCode, setCalendarCode] = useState('')
  const [direction, setDirection] = useState<'Forward' | 'Reverse'>('Forward')
  const [category, setCategory] = useState<'Regional' | 'Intercity' | 'HighSpeed'>('Regional')
  const [stops, setStops] = useState<StopRow[]>([{ stationCode: '', arrival: '', departure: '' }, { stationCode: '', arrival: '', departure: '' }])
  const [submitted, setSubmitted] = useState(false)

  const update = (index: number, patch: Partial<StopRow>) => setStops((rows) => rows.map((row, i) => (i === index ? { ...row, ...patch } : row)))
  const errors = {
    trainNumber: !/^[A-Za-z0-9]{1,10}$/.test(trainNumber) ? 'Letters and digits, up to 10.' : null,
    routeCode: !routeCode ? 'Choose a route.' : null,
    calendarCode: !calendarCode ? 'Choose the days it runs.' : null,
    stops: stops.some((s, i) => !s.stationCode || (i > 0 && parseClock(s.arrival) === null) || (i < stops.length - 1 && parseClock(s.departure) === null)) ? 'Every stop needs a station and the times as HH:mm (the first stop has no arrival, the last no departure).' : null,
  }

  function submit() {
    setSubmitted(true)
    if (Object.values(errors).some(Boolean)) return
    add.mutate(
      {
        trainNumber: trainNumber.trim(),
        routeCode,
        calendarCode,
        direction,
        category,
        stops: stops.map((s, i) => ({ stationCode: s.stationCode, arrival: i === 0 ? null : s.arrival, departure: i === stops.length - 1 ? null : s.departure })),
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="lg"
      title="Add a service"
      description="A new train in this draft. It must follow its route in order."
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={add.isPending}>Cancel</Button>
          <Button onClick={submit} loading={add.isPending}>Add service</Button>
        </>
      }
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Field label="Train number" error={submitted ? errors.trainNumber : null}>
            {(props) => <TextInput {...props} value={trainNumber} maxLength={10} onChange={(event) => setTrainNumber(event.target.value)} />}
          </Field>
          <Field label="Route" error={submitted ? errors.routeCode : null}>
            {(props) => (
              <Select {...props} value={routeCode} onChange={(event) => setRouteCode(event.target.value)}>
                <option value="">Choose a route</option>
                {routeCodes.map((code) => <option key={code} value={code}>{code}</option>)}
              </Select>
            )}
          </Field>
          <Field label="Days it runs" error={submitted ? errors.calendarCode : null}>
            {(props) => (
              <Select {...props} value={calendarCode} onChange={(event) => setCalendarCode(event.target.value)}>
                <option value="">Choose a calendar</option>
                {calendarCodes.map((code) => <option key={code} value={code}>{code}</option>)}
              </Select>
            )}
          </Field>
          <div className="grid grid-cols-2 gap-4">
            <Field label="Direction">
              {(props) => <Select {...props} value={direction} onChange={(event) => setDirection(event.target.value as 'Forward' | 'Reverse')}><option>Forward</option><option>Reverse</option></Select>}
            </Field>
            <Field label="Category">
              {(props) => <Select {...props} value={category} onChange={(event) => setCategory(event.target.value as 'Regional' | 'Intercity' | 'HighSpeed')}><option>Regional</option><option>Intercity</option><option>HighSpeed</option></Select>}
            </Field>
          </div>
        </div>

        <div className="space-y-2">
          <p className="text-[13px] font-semibold">Stops, in order</p>
          {stops.map((stop, index) => (
            <div key={index} className="grid grid-cols-[1fr_6rem_6rem_2rem] items-center gap-2">
              <StationSelect aria-label={`Stop ${index + 1} station`} value={stop.stationCode} onChange={(code) => update(index, { stationCode: code })} placeholder="Choose a station" />
              <TextInput aria-label={`Stop ${index + 1} arrival`} placeholder="Arrives" disabled={index === 0} value={stop.arrival} onChange={(event) => update(index, { arrival: event.target.value })} />
              <TextInput aria-label={`Stop ${index + 1} departure`} placeholder="Departs" disabled={index === stops.length - 1} value={stop.departure} onChange={(event) => update(index, { departure: event.target.value })} />
              <button type="button" aria-label={`Remove stop ${index + 1}`} disabled={stops.length <= 2} className="text-fg-tertiary hover:text-cancelled disabled:opacity-30" onClick={() => setStops((rows) => rows.filter((_, i) => i !== index))}>×</button>
            </div>
          ))}
          <Button variant="ghost" size="sm" onClick={() => setStops((rows) => [...rows.slice(0, -1), { stationCode: '', arrival: '', departure: '' }, rows[rows.length - 1] as StopRow])}>Add a stop</Button>
          {submitted && errors.stops && <p role="alert" className="text-xs text-cancelled">{errors.stops}</p>}
        </div>
      </div>
    </Modal>
  )
}

export function SubmitVersionModal({ versionId, label, open, onClose }: { versionId: string; label: string; open: boolean; onClose: () => void }) {
  const submit = useSubmitVersion(versionId)
  return (
    <ConfirmModal
      open={open}
      onClose={onClose}
      title={`Send ${label} for review`}
      description="A different scheduler must approve it. You can keep editing until they do."
      confirmLabel="Send for review"
      pending={submit.isPending}
      onConfirm={() => submit.mutate(undefined, { onSuccess: onClose })}
    />
  )
}

/** Approve a version in review as a different person, with the date it takes effect. */
export function ApproveVersionModal({ versionId, label, open, onClose }: { versionId: string; label: string; open: boolean; onClose: () => void }) {
  const approve = useApproveVersion(versionId)
  // Suggest a week from now; the person can change it.
  const [date, setDate] = useState(() => todayIso(new Date(Date.now() + 7 * 24 * 3_600_000)))
  const [today] = useState(() => todayIso())
  const comparison = useComparison(versionId, open)
  const invalid = date <= today

  return (
    <ConfirmModal
      open={open}
      onClose={onClose}
      title={`Approve ${label}`}
      description="It replaces the timetable in force from the date you choose. Journeys already generated are not changed."
      confirmLabel="Approve timetable"
      pending={approve.isPending}
      confirmDisabled={invalid}
      onConfirm={() => approve.mutate(date, { onSuccess: onClose })}
    >
      <div className="space-y-4">
        <Field label="Takes effect on" error={invalid ? 'Choose a date after today.' : null}>
          {(props) => <TextInput {...props} type="date" value={date} onChange={(event) => setDate(event.target.value)} />}
        </Field>
        {comparison.isPending ? <Skeleton className="h-4 w-56" /> : comparison.isError ? <ErrorState error={comparison.error} /> : (
          <p className="flex flex-wrap gap-2">
            <StatusPill tone="ontime">{comparison.data.added} added</StatusPill>
            <StatusPill tone="cancelled">{comparison.data.removed} removed</StatusPill>
            <StatusPill tone="delayed">{comparison.data.changed} changed</StatusPill>
            <span className="text-fg-secondary">against {comparison.data.againstLabel}</span>
          </p>
        )}
      </div>
    </ConfirmModal>
  )
}
