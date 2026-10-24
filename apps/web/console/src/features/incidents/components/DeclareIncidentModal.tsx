import { useState } from 'react'
import { Button, Field, Modal, Select, TextArea, TextInput } from '@/shared/components/ui'
import { Permissions, useCan } from '@/shared/auth'
import { StationChecklist } from '@/features/network'
import { useDeclareIncident } from '../hooks/useIncidents'
import type { IncidentSeverity } from '../types'

interface Props {
  open: boolean
  onClose: () => void
  onDeclared: (id: string) => void
}

/** Declare an incident. Critical is the Incident Manager capability and needs a confirmation and a reason (REQ-OPS-007). */
export function DeclareIncidentModal({ open, onClose, onDeclared }: Props) {
  const canCritical = useCan(Permissions.IncidentDeclareCritical)
  const declare = useDeclareIncident()
  const [title, setTitle] = useState('')
  const [severity, setSeverity] = useState<IncidentSeverity>('Major')
  const [stations, setStations] = useState<string[]>([])
  const [reportedBy, setReportedBy] = useState('')
  const [confirm, setConfirm] = useState(false)
  const [reason, setReason] = useState('')
  const [submitted, setSubmitted] = useState(false)

  const critical = severity === 'Critical'
  const errors = {
    title: !title.trim() ? 'Give the incident a short title.' : null,
    reason: critical && !reason.trim() ? 'A critical incident needs a reason.' : null,
    confirm: critical && !confirm ? 'Confirm that this is a critical incident.' : null,
  }

  function submit() {
    setSubmitted(true)
    if (errors.title || errors.reason || errors.confirm) return
    declare.mutate(
      { title: title.trim(), severity, stationCodes: stations, reportedBy: reportedBy.trim() || undefined, confirm, reason: reason.trim() || undefined },
      { onSuccess: (detail) => onDeclared(detail.incident.id) },
    )
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Declare an incident"
      description="You become the owner. Everything you do about it is recorded on its timeline."
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={declare.isPending}>Cancel</Button>
          <Button variant={critical ? 'danger' : 'primary'} onClick={submit} loading={declare.isPending}>Declare incident</Button>
        </>
      }
    >
      <div className="space-y-4">
        <Field label="Title" error={submitted ? errors.title : null}>
          {(props) => <TextInput {...props} value={title} maxLength={120} onChange={(event) => setTitle(event.target.value)} />}
        </Field>
        <Field label="Severity">
          {(props) => (
            <Select {...props} value={severity} onChange={(event) => setSeverity(event.target.value as IncidentSeverity)}>
              <option value="Minor">Minor</option>
              <option value="Major">Major</option>
              {canCritical && <option value="Critical">Critical</option>}
            </Select>
          )}
        </Field>
        <Field label="Reported by (optional)">
          {(props) => <TextInput {...props} value={reportedBy} placeholder="Reported by signaller" maxLength={120} onChange={(event) => setReportedBy(event.target.value)} />}
        </Field>
        <div className="space-y-1.5">
          <p className="text-[13px] font-semibold">Where</p>
          <StationChecklist selected={stations} onChange={setStations} />
        </div>
        {critical && (
          <div className="space-y-4 rounded-card bg-cancelled-bg p-4">
            <Field label="Reason for declaring it critical" error={submitted ? errors.reason : null}>
              {(props) => <TextArea {...props} value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} />}
            </Field>
            <label className="flex items-center gap-2 font-semibold text-cancelled">
              <input type="checkbox" checked={confirm} onChange={(event) => setConfirm(event.target.checked)} />
              I confirm this is a critical incident
            </label>
            {submitted && errors.confirm && <p role="alert" className="text-xs text-cancelled">{errors.confirm}</p>}
          </div>
        )}
      </div>
    </Modal>
  )
}
