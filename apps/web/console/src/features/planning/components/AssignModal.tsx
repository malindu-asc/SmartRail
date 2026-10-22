import { useState } from 'react'
import { Button, ErrorState, Field, Modal, Select, Skeleton, TextArea } from '@/shared/components/ui'
import { useAuth, Permissions } from '@/shared/auth'
import { formatTime } from '@/shared/utils'
import { useAssign, useAssignmentOptions } from '../hooks/usePlanning'

interface Props {
  journeyId: string
  open: boolean
  onClose: () => void
}

/** Pick a driver duty and a unit for a journey. The server checks overlap, rest and availability and lists each check. */
export function AssignModal({ journeyId, open, onClose }: Props) {
  const { can } = useAuth()
  const options = useAssignmentOptions(journeyId, open)
  const assign = useAssign(journeyId)
  const [dutyId, setDutyId] = useState('')
  const [unitId, setUnitId] = useState('')
  const [reason, setReason] = useState('')
  const [acceptRest, setAcceptRest] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  const canDriver = can(Permissions.JourneyDriverAssign)
  const canUnit = can(Permissions.JourneyVehicleAssign)
  const duty = options.data?.drivers.find((d) => d.dutyId === dutyId)
  const restFailed = duty?.checks.some((c) => !c.passed && c.code.toLowerCase().includes('rest')) ?? false

  function submit() {
    setSubmitted(true)
    if (!reason.trim() || (!dutyId && !unitId) || !options.data) return
    assign.mutate(
      {
        unitId: unitId || undefined,
        dutyId: dutyId || undefined,
        reason: reason.trim(),
        acceptRestConflict: acceptRest,
        expectedVersion: options.data.version,
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={options.data ? `Assign ${options.data.trainNumber}` : 'Assign driver and unit'}
      description={options.data ? `${options.data.route}, departs ${formatTime(options.data.departsUtc)}` : undefined}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={assign.isPending}>Close</Button>
          <Button onClick={submit} loading={assign.isPending} disabled={!options.data}>Save assignment</Button>
        </>
      }
    >
      {options.isPending ? (
        <div role="status" aria-label="Loading options" className="space-y-3">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      ) : options.isError ? (
        <ErrorState error={options.error} onRetry={() => void options.refetch()} />
      ) : (
        <div className="space-y-4">
          {canDriver && (
            <Field label="Driver" error={submitted && !dutyId && !unitId ? 'Choose a driver or a unit.' : null}>
              {(props) => (
                <Select {...props} value={dutyId} onChange={(event) => { setDutyId(event.target.value); setAcceptRest(false) }}>
                  <option value="">Keep as it is</option>
                  {options.data.drivers.map((driver) => (
                    <option key={driver.dutyId} value={driver.dutyId}>
                      {driver.driverName} ({formatTime(driver.dutyFromUtc)} to {formatTime(driver.dutyToUtc)}){driver.eligible ? '' : ' – check needed'}
                    </option>
                  ))}
                </Select>
              )}
            </Field>
          )}
          {duty && (
            <ul className="space-y-1 rounded-card bg-soft p-3" aria-label="Checks">
              {duty.checks.map((check) => (
                <li key={check.code} className={check.passed ? 'text-on-time' : 'text-cancelled'}>
                  {check.passed ? '✓' : '✗'} {check.label}
                </li>
              ))}
              {duty.note && <li className="text-fg-secondary">{duty.note}</li>}
            </ul>
          )}
          {restFailed && (
            <label className="flex items-center gap-2 font-semibold text-delayed">
              <input type="checkbox" checked={acceptRest} onChange={(event) => setAcceptRest(event.target.checked)} />
              Assign anyway, the rest rule is not met
            </label>
          )}
          {canUnit && (
            <Field label="Unit">
              {(props) => (
                <Select {...props} value={unitId} onChange={(event) => setUnitId(event.target.value)}>
                  <option value="">Keep as it is</option>
                  {options.data.units.map((unit) => (
                    <option key={unit.unitId} value={unit.unitId} disabled={!unit.available}>
                      {unit.code} {unit.name}{unit.available ? '' : ` – ${unit.note || 'not available'}`}
                    </option>
                  ))}
                </Select>
              )}
            </Field>
          )}
          <Field label="Reason" error={submitted && !reason.trim() ? 'Give a reason.' : null}>
            {(props) => <TextArea {...props} value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} />}
          </Field>
        </div>
      )}
    </Modal>
  )
}
