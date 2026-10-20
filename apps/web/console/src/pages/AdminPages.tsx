import { Plus } from 'lucide-react'
import { useState } from 'react'
import {
  AdminDashboardView,
  DevicesTable,
  EditUserModal,
  InviteUserModal,
  RegisterDeviceModal,
  RevokeDeviceModal,
  RolesMatrix,
  UsersTable,
  useAdminDashboard,
  useDevices,
  useRoleMatrix,
  useUsers,
  type Device,
  type StaffAccount,
} from '@/features/admin'
import { StationSelect } from '@/features/network'
import { Can, Permissions, Roles, roleLabel } from '@/shared/auth'
import { Button, CardSkeleton, ErrorState, FilterBar, PageHeader, Select, StatCardsSkeleton, TableSkeleton, TextInput } from '@/shared/components/ui'
import { useQueryParam } from '@/shared/hooks'

export function AdminDashboardPage() {
  const dashboard = useAdminDashboard()
  return (
    <>
      <PageHeader title="Administration" description="Who can sign in, what they can do, and whether the devices are healthy." />
      {dashboard.isPending ? (
        <div className="space-y-4"><StatCardsSkeleton count={4} /><CardSkeleton lines={5} /></div>
      ) : dashboard.isError ? (
        <ErrorState error={dashboard.error} onRetry={() => void dashboard.refetch()} />
      ) : (
        <AdminDashboardView data={dashboard.data} />
      )}
    </>
  )
}

const STATUSES = ['Active', 'Locked', 'Disabled', 'Leaver']

export function UsersPage() {
  const [role, setRole] = useQueryParam('role')
  const [status, setStatus] = useQueryParam('status', 'Active')
  const [station, setStation] = useQueryParam('station')
  const [query, setQuery] = useQueryParam('q')
  const [inviting, setInviting] = useState(false)
  const [editing, setEditing] = useState<StaffAccount | null>(null)
  const users = useUsers({ role, status: status === 'all' ? undefined : status, station, q: query })

  return (
    <>
      <PageHeader
        title="Users"
        description="Staff accounts. Passengers register themselves and are not listed here."
        actions={
          <Can permission={Permissions.RoleAssign}>
            <Button icon={<Plus aria-hidden className="size-4" />} onClick={() => setInviting(true)}>Invite user</Button>
          </Can>
        }
      />
      <FilterBar>
        <div className="w-48">
          <Select pill aria-label="Role" value={role} onChange={(event) => setRole(event.target.value)}>
            <option value="">All roles</option>
            {[Roles.Controller, Roles.Scheduler, Roles.StationMaster, Roles.Driver, Roles.Inspector, Roles.Admin].map((r) => <option key={r} value={r}>{roleLabel(r)}</option>)}
          </Select>
        </div>
        <div className="w-40">
          <Select pill aria-label="Status" value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="all">Any status</option>
            {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
          </Select>
        </div>
        <div className="w-48"><StationSelect pill aria-label="Station" value={station} onChange={setStation} placeholder="All stations" /></div>
        <TextInput aria-label="Search" type="search" placeholder="Search name or email" className="w-64 rounded-full" value={query} onChange={(event) => setQuery(event.target.value)} />
      </FilterBar>

      {users.isPending ? (
        <TableSkeleton columns={6} label="Loading staff accounts" />
      ) : users.isError ? (
        <ErrorState error={users.error} onRetry={() => void users.refetch()} />
      ) : (
        <UsersTable users={users.data} onEdit={setEditing} />
      )}

      {inviting && <InviteUserModal open onClose={() => setInviting(false)} />}
      {editing && <EditUserModal key={editing.id} user={editing} onClose={() => setEditing(null)} />}
    </>
  )
}

export function RolesPage() {
  const matrix = useRoleMatrix()
  return (
    <>
      <PageHeader title="Roles and permissions" description="Who can do what. No role can both run trains and manage access." />
      {matrix.isPending ? <TableSkeleton columns={7} rows={8} label="Loading roles" /> : matrix.isError ? <ErrorState error={matrix.error} onRetry={() => void matrix.refetch()} /> : <RolesMatrix matrix={matrix.data} />}
    </>
  )
}

export function DevicesPage() {
  const [type, setType] = useQueryParam('type')
  const [health, setHealth] = useQueryParam('health')
  const [station, setStation] = useQueryParam('station')
  const [registering, setRegistering] = useState(false)
  const [revoking, setRevoking] = useState<Device | null>(null)
  const devices = useDevices({ type, health, station })

  return (
    <>
      <PageHeader
        title="Devices"
        description="Platform displays, concourse boards, gate scanners and terminals, and when each was last heard from."
        actions={
          <Can permission={Permissions.DeviceProvision}>
            <Button icon={<Plus aria-hidden className="size-4" />} onClick={() => setRegistering(true)}>Register device</Button>
          </Can>
        }
      />
      <FilterBar>
        <div className="w-44">
          <Select pill aria-label="Type" value={type} onChange={(event) => setType(event.target.value)}>
            <option value="">All types</option>
            <option value="displays">Displays</option>
            <option value="gates">Gate scanners</option>
            <option value="terminals">Terminals</option>
          </Select>
        </div>
        <div className="w-44">
          <Select pill aria-label="Health" value={health} onChange={(event) => setHealth(event.target.value)}>
            <option value="">Any health</option>
            {['Online', 'Late', 'Offline', 'Pending', 'Revoked'].map((h) => <option key={h} value={h}>{h}</option>)}
          </Select>
        </div>
        <div className="w-48"><StationSelect pill aria-label="Station" value={station} onChange={setStation} placeholder="All stations" /></div>
      </FilterBar>

      {devices.isPending ? (
        <TableSkeleton columns={6} label="Loading devices" />
      ) : devices.isError ? (
        <ErrorState error={devices.error} onRetry={() => void devices.refetch()} />
      ) : (
        <DevicesTable devices={devices.data.items} onRevoke={setRevoking} />
      )}

      {registering && <RegisterDeviceModal open onClose={() => setRegistering(false)} />}
      {revoking && <RevokeDeviceModal key={revoking.id} device={revoking} onClose={() => setRevoking(null)} />}
    </>
  )
}
