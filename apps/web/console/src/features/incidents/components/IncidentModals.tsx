import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Button, ConfirmModal, Field, Modal, Select, TextArea } from '@/shared/components/ui'
import { usePeople } from '@/features/people'
import { useDisruptionList } from '@/features/disruptions'
import { useEmergencyTermination, useHandOverIncident, useIncidents, useLinkDisruption, useResolveIncident } from '../hooks/useIncidents'
import type { Incident } from '../types'

interface IncidentModalProps {
  incident: Incident
  open: boolean
  onClose: () => void
}

/** Give the incident to another controller with a note. An incident is never left without an owner. */
export function HandOverModal({ incident, open, onClose }: IncidentModalProps) {
  const people = usePeople('controller', open)
  const handOver = useHandOverIncident(incident.id)
  const [owner, setOwner] = useState('')
  const [note, setNote] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function submit() {
    setSubmitted(true)
    if (!owner || !note.trim()) return
    handOver.mutate({ newOwnerSubject: owner, note: note.trim() }, { onSuccess: onClose })
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Hand over this incident"
      description="The new owner is responsible from now on. Say what they need to know."
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={handOver.isPending}>Keep it</Button>
          <Button onClick={submit} loading={handOver.isPending}>Hand over</Button>
        </>
      }
    >
      <div className="space-y-4">
        <Field label="New owner" error={submitted && !owner ? 'Choose a controller.' : null}>
          {(props) => (
            <Select {...props} value={owner} onChange={(event) => setOwner(event.target.value)} disabled={people.isPending}>
              <option value="">Choose a controller</option>
              {(people.data ?? []).filter((p) => p.subject !== incident.ownerSubject).map((p) => <option key={p.subject} value={p.subject}>{p.name}</option>)}
            </Select>
          )}
        </Field>
        <Field label="Handover note" error={submitted && !note.trim() ? 'A handover needs a note.' : null}>
          {(props) => <TextArea {...props} value={note} maxLength={300} onChange={(event) => setNote(event.target.value)} />}
        </Field>
      </div>
    </Modal>
  )
}

export function ResolveIncidentModal({ incident, open, onClose }: IncidentModalProps) {
  const resolve = useResolveIncident(incident.id)
  const [note, setNote] = useState('')
  const [submitted, setSubmitted] = useState(false)

  return (
    <ConfirmModal
      open={open}
      onClose={onClose}
      title="Resolve this incident"
      description="The incident closes and can no longer be changed."
      confirmLabel="Resolve incident"
      pending={resolve.isPending}
      onConfirm={() => {
        setSubmitted(true)
        if (note.trim()) resolve.mutate(note.trim(), { onSuccess: onClose })
      }}
    >
      <Field label="What was the outcome?" error={submitted && !note.trim() ? 'Write the outcome.' : null}>
        {(props) => <TextArea {...props} value={note} maxLength={300} onChange={(event) => setNote(event.target.value)} />}
      </Field>
    </ConfirmModal>
  )
}

/** Link the passenger notice that goes with an incident. */
export function LinkDisruptionModal({ incident, open, onClose }: IncidentModalProps) {
  const disruptions = useDisruptionList('active')
  const link = useLinkDisruption(incident.id)
  const [id, setId] = useState('')

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Link a passenger notice"
      description="Passengers see the notice; the incident keeps track of it."
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={link.isPending}>Cancel</Button>
          <Button onClick={() => id && link.mutate(id, { onSuccess: onClose })} loading={link.isPending} disabled={!id}>Link notice</Button>
        </>
      }
    >
      <Field label="Notice">
        {(props) => (
          <Select {...props} value={id} onChange={(event) => setId(event.target.value)} disabled={disruptions.isPending}>
            <option value="">Choose a notice</option>
            {(disruptions.data?.items ?? []).map((d) => <option key={d.id} value={d.id}>{d.title}</option>)}
          </Select>
        )}
      </Field>
    </Modal>
  )
}

interface EmergencyProps {
  journeyId: string
  trainNumber: string
  open: boolean
  onClose: () => void
}

/**
 * Terminate a train in an emergency. It is always part of an incident (so it is on a timeline and has an owner), needs a reason
 * and a confirmation. Passengers see only the approved wording (REQ-OPS-008).
 */
export function EmergencyTerminateModal({ journeyId, trainNumber, open, onClose }: EmergencyProps) {
  const incidents = useIncidents(true)
  const terminate = useEmergencyTermination()
  const [incidentId, setIncidentId] = useState('')
  const [reason, setReason] = useState('')
  const [confirm, setConfirm] = useState(false)
  const [submitted, setSubmitted] = useState(false)
  const open_ = incidents.data ?? []

  function submit() {
    setSubmitted(true)
    if (!incidentId || !reason.trim() || !confirm) return
    terminate.mutate({ incidentId, journeyId, reason: reason.trim() }, { onSuccess: onClose })
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={`Emergency termination of ${trainNumber}`}
      description="The train is ended now. Passengers see only: 'Train has stopped because of an emergency. Please follow staff instructions.'"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={terminate.isPending}>Do not terminate</Button>
          <Button variant="danger" onClick={submit} loading={terminate.isPending}>Terminate train</Button>
        </>
      }
    >
      <div className="space-y-4">
        {open_.length === 0 && !incidents.isPending ? (
          <p role="alert" className="rounded-card bg-delayed-bg p-4 text-delayed">
            An emergency termination belongs to an incident. <Link to="/incidents" className="font-semibold underline">Declare an incident first</Link>.
          </p>
        ) : (
          <Field label="Incident" error={submitted && !incidentId ? 'Choose the incident this belongs to.' : null}>
            {(props) => (
              <Select {...props} value={incidentId} onChange={(event) => setIncidentId(event.target.value)} disabled={incidents.isPending}>
                <option value="">Choose an incident</option>
                {open_.map((i) => <option key={i.id} value={i.id}>{i.title}</option>)}
              </Select>
            )}
          </Field>
        )}
        <Field label="Reason" error={submitted && !reason.trim() ? 'A reason is required.' : null}>
          {(props) => <TextArea {...props} value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} />}
        </Field>
        <label className="flex items-center gap-2 font-semibold text-cancelled">
          <input type="checkbox" checked={confirm} onChange={(event) => setConfirm(event.target.checked)} />
          I confirm this is an emergency
        </label>
        {submitted && !confirm && <p role="alert" className="text-xs text-cancelled">Confirm that this is an emergency.</p>}
      </div>
    </Modal>
  )
}
