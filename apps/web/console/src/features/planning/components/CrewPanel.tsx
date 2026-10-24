import { useState } from 'react'
import { Button, EmptyState, Field, Modal, Select, StatusPill, Table, Td, TextInput, Th, THead, Tr } from '@/shared/components/ui'
import { Can, Permissions } from '@/shared/auth'
import { formatTime, localInputToUtc, toLocalInput } from '@/shared/utils'
import { useCreateDuty } from '../hooks/usePlanning'
import type { CrewRow } from '../types'

export function CrewTable({ crew }: { crew: CrewRow[] }) {
  if (crew.length === 0) return <EmptyState title="No duties for this day" description="Roster a driver to make them available for assignments." />

  return (
    <Table caption="Driver duties">
      <THead>
        <Th>Driver</Th>
        <Th>Shift</Th>
        <Th>State</Th>
        <Th>Journeys</Th>
      </THead>
      <tbody>
        {crew.map((row) => (
          <Tr key={row.dutyId}>
            <Td className="font-semibold">{row.driverName}</Td>
            <Td>{formatTime(row.fromUtc)} to {formatTime(row.toUtc)}</Td>
            <Td><StatusPill tone={row.state === 'Orphaned' ? 'cancelled' : 'neutral'}>{row.state}</StatusPill></Td>
            <Td className="text-fg-secondary">{row.journeyTimes.length === 0 ? '–' : row.journeyTimes.join(', ')}</Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}

interface RosterProps {
  open: boolean
  onClose: () => void
  /** Drivers the scheduler can roster: those already on the duty list. */
  drivers: { subject: string; name: string }[]
}

/** Roster a driver for a shift so they can be assigned to journeys. */
export function RosterDutyModal({ open, onClose, drivers }: RosterProps) {
  const create = useCreateDuty()
  const [driver, setDriver] = useState('')
  const [from, setFrom] = useState(() => toLocalInput(new Date()))
  const [to, setTo] = useState(() => toLocalInput(new Date(Date.now() + 8 * 3_600_000)))
  const [submitted, setSubmitted] = useState(false)
  const invalid = new Date(to) <= new Date(from)

  function submit() {
    setSubmitted(true)
    if (!driver || invalid) return
    create.mutate({ driverSubject: driver, fromUtc: localInputToUtc(from), toUtc: localInputToUtc(to) }, { onSuccess: onClose })
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Roster a driver"
      description="The shift makes the driver available for assignments in that window."
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={create.isPending}>Cancel</Button>
          <Button onClick={submit} loading={create.isPending}>Roster driver</Button>
        </>
      }
    >
      <div className="space-y-4">
        <Field label="Driver" error={submitted && !driver ? 'Choose a driver.' : null}>
          {(props) => (
            <Select {...props} value={driver} onChange={(event) => setDriver(event.target.value)}>
              <option value="">Choose a driver</option>
              {drivers.map((d) => <option key={d.subject} value={d.subject}>{d.name}</option>)}
            </Select>
          )}
        </Field>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Starts">{(props) => <TextInput {...props} type="datetime-local" value={from} onChange={(event) => setFrom(event.target.value)} />}</Field>
          <Field label="Ends" error={submitted && invalid ? 'Must be after the start.' : null}>
            {(props) => <TextInput {...props} type="datetime-local" value={to} onChange={(event) => setTo(event.target.value)} />}
          </Field>
        </div>
      </div>
    </Modal>
  )
}

/** The "Roster driver" button, shown only to people who may assign drivers. */
export function RosterButton({ onClick }: { onClick: () => void }) {
  return (
    <Can permission={Permissions.JourneyDriverAssign}>
      <Button variant="secondary" onClick={onClick}>Roster a driver</Button>
    </Can>
  )
}
