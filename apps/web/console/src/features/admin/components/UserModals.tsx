import { useState } from 'react'
import { Button, ConfirmModal, Field, Modal, Select, StatusPill, TextArea, TextInput } from '@/shared/components/ui'
import { roleLabel, Roles } from '@/shared/auth'
import { StationChecklist } from '@/features/network'
import { useDisableUser, useEnableUser, useInviteUser, useUnlockUser, useUpdateUser } from '../hooks/useAdmin'
import type { StaffAccount, StaffInvite } from '../types'

const STAFF_ROLES = [Roles.Controller, Roles.Scheduler, Roles.StationMaster, Roles.Driver, Roles.Inspector, Roles.Admin]
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/** Invite a staff member with one role. They set their own password and two-step code. */
export function InviteUserModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const invite = useInviteUser()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [role, setRole] = useState('')
  const [stations, setStations] = useState<string[]>([])
  const [submitted, setSubmitted] = useState(false)
  const [result, setResult] = useState<StaffInvite | null>(null)

  const errors = {
    fullName: !fullName.trim() ? 'Enter their full name.' : null,
    email: !EMAIL.test(email.trim()) ? 'Enter a valid email address.' : null,
    role: !role ? 'Choose a role.' : null,
    stations: role === Roles.StationMaster && stations.length === 0 ? 'A station master needs at least one station.' : null,
  }

  function reset() {
    setFullName('')
    setEmail('')
    setRole('')
    setStations([])
    setSubmitted(false)
    setResult(null)
  }

  function close() {
    onClose()
    reset()
  }

  function submit() {
    setSubmitted(true)
    if (Object.values(errors).some(Boolean)) return
    invite.mutate({ fullName: fullName.trim(), email: email.trim(), role, stationCodes: role === Roles.StationMaster ? stations : [] }, { onSuccess: setResult })
  }

  if (result) {
    return (
      <Modal open={open} onClose={close} size="sm" title="Invitation created" footer={<Button onClick={close}>Done</Button>}>
        <p>
          {result.emailSent ? `An email was sent to ${result.account.email}.` : `No email service is set up here, so ${result.account.fullName} needs the temporary password below.`}
        </p>
        {result.temporaryPassword && (
          <p className="mt-3 rounded-card bg-soft p-3">
            Temporary password: <code className="font-mono font-semibold">{result.temporaryPassword}</code>
            <span className="mt-1 block text-xs text-fg-tertiary">Shown once. They must change it at first sign-in.</span>
          </p>
        )}
      </Modal>
    )
  }

  return (
    <Modal
      open={open}
      onClose={close}
      title="Invite a person"
      description="They set their own password and two-step code. You choose their role."
      footer={
        <>
          <Button variant="secondary" onClick={close} disabled={invite.isPending}>Cancel</Button>
          <Button onClick={submit} loading={invite.isPending}>Send invitation</Button>
        </>
      }
    >
      <div className="space-y-4">
        <Field label="Full name" error={submitted ? errors.fullName : null}>
          {(props) => <TextInput {...props} value={fullName} maxLength={100} onChange={(event) => setFullName(event.target.value)} />}
        </Field>
        <Field label="Email" error={submitted ? errors.email : null}>
          {(props) => <TextInput {...props} type="email" value={email} maxLength={200} onChange={(event) => setEmail(event.target.value)} />}
        </Field>
        <Field label="Role" error={submitted ? errors.role : null}>
          {(props) => (
            <Select {...props} value={role} onChange={(event) => setRole(event.target.value)}>
              <option value="">Choose a role</option>
              {STAFF_ROLES.map((r) => <option key={r} value={r}>{roleLabel(r)}</option>)}
            </Select>
          )}
        </Field>
        {role === Roles.StationMaster && (
          <div className="space-y-1.5">
            <p className="text-[13px] font-semibold">Stations</p>
            <StationChecklist selected={stations} onChange={setStations} />
            {submitted && errors.stations && <p role="alert" className="text-xs text-cancelled">{errors.stations}</p>}
          </div>
        )}
        {role === Roles.Admin && <p role="note" className="rounded-card bg-delayed-bg p-3 text-delayed">An administrator manages access but cannot operate trains.</p>}
      </div>
    </Modal>
  )
}

interface UserProps {
  user: StaffAccount | null
  onClose: () => void
}

/** Edit a person. Changing the role needs a reason, and you cannot change your own access. */
export function EditUserModal({ user, onClose }: UserProps) {
  const update = useUpdateUser(user?.id ?? '')
  const [fullName, setFullName] = useState(user?.fullName ?? '')
  const [email, setEmail] = useState(user?.email ?? '')
  const [role, setRole] = useState(user?.roles[0] ?? '')
  const [stations, setStations] = useState<string[]>(user?.stationCodes ?? [])
  const [reason, setReason] = useState('')
  const [dialog, setDialog] = useState<'disable' | 'unlock' | 'enable' | null>(null)
  const [submitted, setSubmitted] = useState(false)

  const roleChanged = user !== null && role !== (user.roles[0] ?? '')
  const errors = {
    fullName: !fullName.trim() ? 'Enter their full name.' : null,
    email: !EMAIL.test(email.trim()) ? 'Enter a valid email address.' : null,
    reason: roleChanged && !reason.trim() ? 'Changing a role needs a reason.' : null,
    stations: role === Roles.StationMaster && stations.length === 0 ? 'A station master needs at least one station.' : null,
  }

  function save() {
    setSubmitted(true)
    if (!user || Object.values(errors).some(Boolean)) return
    update.mutate(
      { fullName: fullName.trim(), email: email.trim(), role: roleChanged ? role : undefined, stationCodes: role === Roles.StationMaster ? stations : undefined, reason: roleChanged ? reason.trim() : undefined },
      { onSuccess: onClose },
    )
  }

  return (
    <>
      <Modal
        open={user !== null && dialog === null}
        onClose={onClose}
        title={user ? `Edit ${user.fullName}` : 'Edit person'}
        footer={
          <>
            <Button variant="secondary" onClick={onClose} disabled={update.isPending}>Cancel</Button>
            <Button onClick={save} loading={update.isPending}>Save changes</Button>
          </>
        }
      >
        {user && (
          <div className="space-y-4">
            <Field label="Full name" error={submitted ? errors.fullName : null}>
              {(props) => <TextInput {...props} value={fullName} maxLength={100} onChange={(event) => setFullName(event.target.value)} />}
            </Field>
            <Field label="Email" error={submitted ? errors.email : null}>
              {(props) => <TextInput {...props} type="email" value={email} onChange={(event) => setEmail(event.target.value)} />}
            </Field>
            <Field label="Role">
              {(props) => (
                <Select {...props} value={role} onChange={(event) => setRole(event.target.value)}>
                  {STAFF_ROLES.map((r) => <option key={r} value={r}>{roleLabel(r)}</option>)}
                </Select>
              )}
            </Field>
            {role === Roles.StationMaster && (
              <div className="space-y-1.5">
                <p className="text-[13px] font-semibold">Stations</p>
                <StationChecklist selected={stations} onChange={setStations} />
                {submitted && errors.stations && <p role="alert" className="text-xs text-cancelled">{errors.stations}</p>}
              </div>
            )}
            {roleChanged && (
              <Field label="Reason for the role change" error={submitted ? errors.reason : null}>
                {(props) => <TextArea {...props} value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} />}
              </Field>
            )}
            <div className="flex flex-wrap items-center gap-3 border-t border-line pt-4">
              <StatusPill tone={user.status === 'Locked' ? 'cancelled' : user.status === 'Disabled' ? 'stale' : 'ontime'}>{user.status}</StatusPill>
              {user.status === 'Locked' && <Button size="sm" variant="secondary" onClick={() => setDialog('unlock')}>Unlock account</Button>}
              {user.status === 'Disabled' ? (
                <Button size="sm" variant="secondary" onClick={() => setDialog('enable')}>Enable access</Button>
              ) : (
                <Button size="sm" variant="danger" onClick={() => setDialog('disable')}>Disable access</Button>
              )}
            </div>
          </div>
        )}
      </Modal>
      {user && <ReasonDialogs user={user} dialog={dialog} onDone={() => { setDialog(null); onClose() }} onBack={() => setDialog(null)} />}
    </>
  )
}

function ReasonDialogs({ user, dialog, onDone, onBack }: { user: StaffAccount; dialog: 'disable' | 'unlock' | 'enable' | null; onDone: () => void; onBack: () => void }) {
  const disable = useDisableUser(user.id)
  const unlock = useUnlockUser(user.id)
  const enable = useEnableUser(user.id)
  const [reason, setReason] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const missing = submitted && !reason.trim()

  return (
    <>
      <ConfirmModal
        open={dialog === 'disable'}
        onClose={onBack}
        title={`Disable ${user.fullName}`}
        description="Their sign-in stops at once and their sessions end. Their records stay."
        confirmLabel="Disable access"
        tone="danger"
        pending={disable.isPending}
        onConfirm={() => { setSubmitted(true); if (reason.trim()) disable.mutate(reason.trim(), { onSuccess: onDone }) }}
      >
        <Field label="Reason" error={missing ? 'Give a reason.' : null}>{(props) => <TextArea {...props} value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} />}</Field>
      </ConfirmModal>
      <ConfirmModal
        open={dialog === 'unlock'}
        onClose={onBack}
        title={`Unlock ${user.fullName}`}
        description="They can try to sign in again."
        confirmLabel="Unlock account"
        pending={unlock.isPending}
        onConfirm={() => { setSubmitted(true); if (reason.trim()) unlock.mutate(reason.trim(), { onSuccess: onDone }) }}
      >
        <Field label="Reason" error={missing ? 'Give a reason.' : null}>{(props) => <TextArea {...props} value={reason} maxLength={300} onChange={(event) => setReason(event.target.value)} />}</Field>
      </ConfirmModal>
      <ConfirmModal
        open={dialog === 'enable'}
        onClose={onBack}
        title={`Enable ${user.fullName}`}
        description="They can sign in again."
        confirmLabel="Enable access"
        pending={enable.isPending}
        onConfirm={() => enable.mutate(undefined, { onSuccess: onDone })}
      />
    </>
  )
}
