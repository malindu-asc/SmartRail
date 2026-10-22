import { Pencil } from 'lucide-react'
import { Button, EmptyState, StatusPill, Table, Td, Th, THead, Tr, toneFor } from '@/shared/components/ui'
import { roleLabel } from '@/shared/auth'
import { formatDateTime, initials } from '@/shared/utils'
import type { StaffAccount } from '../types'

interface Props {
  users: StaffAccount[]
  onEdit: (user: StaffAccount) => void
}

function StatusCell({ user }: { user: StaffAccount }) {
  if (user.status === 'Locked') return <StatusPill tone="cancelled">Locked · {user.failedSignIns} failed</StatusPill>
  if (user.status === 'Leaver') return <StatusPill tone="delayed">Leaver · still active</StatusPill>
  return <StatusPill tone={toneFor(user.status)}>{user.status}</StatusPill>
}

/** Staff accounts. Locked accounts and leavers who still have access are shaded so they are not missed. */
export function UsersTable({ users, onEdit }: Props) {
  if (users.length === 0) return <EmptyState title="No one matches" description="Change the filters, or invite a person." />

  return (
    <Table caption="Staff accounts">
      <THead>
        <Th>Name</Th>
        <Th>Email</Th>
        <Th>Role</Th>
        <Th>Status</Th>
        <Th>Last sign-in</Th>
        <Th><span className="sr-only">Actions</span></Th>
      </THead>
      <tbody>
        {users.map((user) => (
          <Tr key={user.id} tone={user.status === 'Locked' ? 'danger' : user.status === 'Leaver' ? 'attention' : undefined}>
            <Td>
              <span className="flex items-center gap-3">
                <span aria-hidden className="flex size-8 items-center justify-center rounded-full bg-soft text-xs font-bold text-fg-secondary">{initials(user.fullName)}</span>
                <span className="font-semibold">{user.fullName}</span>
              </span>
            </Td>
            <Td className="text-fg-secondary">{user.email}</Td>
            <Td>
              {user.roles.map((role) => <StatusPill key={role} className="mr-1">{roleLabel(role)}</StatusPill>)}
              {user.stationCodes.length > 0 && <span className="ml-1 text-xs text-fg-tertiary">{user.stationCodes.join(', ')}</span>}
            </Td>
            <Td><StatusCell user={user} /></Td>
            <Td className="whitespace-nowrap text-fg-secondary">{user.lastSignInUtc ? formatDateTime(user.lastSignInUtc) : 'Never'}</Td>
            <Td className="text-right">
              <Button size="sm" variant="secondary" icon={<Pencil aria-hidden className="size-3.5" />} onClick={() => onEdit(user)}>Edit</Button>
            </Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
