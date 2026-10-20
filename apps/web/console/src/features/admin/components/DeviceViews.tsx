import { useState } from 'react'
import { Button, ConfirmModal, EmptyState, Field, Modal, Select, StatusPill, Table, Td, TextArea, TextInput, Th, THead, Tr, toneFor } from '@/shared/components/ui'
import { Can, Permissions } from '@/shared/auth'
import { StationSelect } from '@/features/network'
import { formatAge, formatDateTime, humanize } from '@/shared/utils'
import { useRegisterDevice, useRevokeDevice } from '../hooks/useAdmin'
import type { Device } from '../types'

const TYPE_LABEL: Record<string, string> = { PlatformDisplay: 'Platform display', ConcourseBoard: 'Concourse board', GateScanner: 'Gate scanner', Terminal: 'Terminal' }

export function DevicesTable({ devices, onRevoke }: { devices: Device[]; onRevoke: (device: Device) => void }) {
  if (devices.length === 0) return <EmptyState title="No devices match" description="Register a display, gate scanner or terminal to see its health here." />

  return (
    <Table caption="Devices">
      <THead>
        <Th>Device</Th>
        <Th>Type</Th>
        <Th>Station</Th>
        <Th>Health</Th>
        <Th>Last heartbeat</Th>
        <Th><span className="sr-only">Actions</span></Th>
      </THead>
      <tbody>
        {devices.map((device) => (
          <Tr key={device.id} tone={device.health === 'Offline' ? 'danger' : device.health === 'Late' ? 'attention' : undefined}>
            <Td className="font-semibold">{device.name}{device.location && <span className="block text-xs font-normal text-fg-tertiary">{device.location}</span>}</Td>
            <Td>{TYPE_LABEL[device.type] ?? humanize(device.type)}</Td>
            <Td>{device.stationName}</Td>
            <Td><StatusPill tone={toneFor(device.health)}>{device.health}</StatusPill></Td>
            <Td className="text-fg-secondary">
              {device.lastHeartbeatUtc ? `${formatAge(device.lastHeartbeatAgeSeconds)} ago` : 'Never heard from'}
              {device.revokedAt && <span className="block text-xs">Revoked {formatDateTime(device.revokedAt)}: {device.revokedReason}</span>}
            </Td>
            <Td className="text-right">
              <Can permission={Permissions.DeviceRevoke}>
                {device.health !== 'Revoked' && <Button size="sm" variant="secondary" onClick={() => onRevoke(device)}>Revoke</Button>}
              </Can>
            </Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}

/** Register a device on the list. It starts Pending and becomes Active when it is first heard from. */
export function RegisterDeviceModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const register = useRegisterDevice()
  const [name, setName] = useState('')
  const [type, setType] = useState('PlatformDisplay')
  const [station, setStation] = useState('')
  const [location, setLocation] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function submit() {
    setSubmitted(true)
    if (!name.trim() || !station) return
    register.mutate({ name: name.trim(), type, stationCode: station, location: location.trim() || undefined }, {
      onSuccess: () => {
        setName(''); setStation(''); setLocation(''); setSubmitted(false)
        onClose()
      },
    })
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Register a device"
      description="It appears on the list as Pending until it is first heard from."
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={register.isPending}>Cancel</Button>
          <Button onClick={submit} loading={register.isPending}>Register device</Button>
        </>
      }
    >
      <div className="space-y-4">
        <Field label="Name" error={submitted && !name.trim() ? 'Give the device a name.' : null}>
          {(props) => <TextInput {...props} value={name} maxLength={80} placeholder="Central platform 1 display" onChange={(event) => setName(event.target.value)} />}
        </Field>
        <Field label="Type">
          {(props) => (
            <Select {...props} value={type} onChange={(event) => setType(event.target.value)}>
              {Object.entries(TYPE_LABEL).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </Select>
          )}
        </Field>
        <Field label="Station" error={submitted && !station ? 'Choose a station.' : null}>
          {(props) => <StationSelect {...props} value={station} onChange={setStation} placeholder="Choose a station" />}
        </Field>
        <Field label="Where in the station (optional)">
          {(props) => <TextInput {...props} value={location} maxLength={120} placeholder="Platform 1" onChange={(event) => setLocation(event.target.value)} />}
        </Field>
      </div>
    </Modal>
  )
}

export function RevokeDeviceModal({ device, onClose }: { device: Device | null; onClose: () => void }) {
  const revoke = useRevokeDevice(device?.id ?? '')
  const [reason, setReason] = useState('')
  const [submitted, setSubmitted] = useState(false)

  return (
    <ConfirmModal
      open={device !== null}
      onClose={onClose}
      title={`Revoke ${device?.name ?? 'device'}`}
      description="It is taken off the list. The record stays for the audit trail."
      confirmLabel="Revoke device"
      tone="danger"
      pending={revoke.isPending}
      onConfirm={() => {
        setSubmitted(true)
        if (reason.trim()) revoke.mutate(reason.trim(), { onSuccess: () => { setReason(''); setSubmitted(false); onClose() } })
      }}
    >
      <Field label="Why?" error={submitted && !reason.trim() ? 'Give a reason.' : null}>
        {(props) => <TextArea {...props} value={reason} maxLength={300} placeholder="Stolen, replaced, decommissioned" onChange={(event) => setReason(event.target.value)} />}
      </Field>
    </ConfirmModal>
  )
}
