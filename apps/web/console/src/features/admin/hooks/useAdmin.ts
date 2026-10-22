import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'
import { useAction } from '@/shared/hooks'
import type { AdminDashboard, DeviceFilters, DeviceList, RoleMatrix, StaffAccount, StaffInvite, UserFilters } from '../types'

export const adminKeys = {
  all: ['admin'] as const,
  dashboard: () => [...adminKeys.all, 'dashboard'] as const,
  users: (filters: UserFilters) => [...adminKeys.all, 'users', filters] as const,
  roles: () => [...adminKeys.all, 'roles'] as const,
  devices: (filters: DeviceFilters) => [...adminKeys.all, 'devices', filters] as const,
}

export function useAdminDashboard() {
  return useQuery({ queryKey: adminKeys.dashboard(), queryFn: () => api.get<AdminDashboard>('/admin/dashboard'), refetchInterval: 30_000 })
}

export function useUsers(filters: UserFilters) {
  return useQuery({
    queryKey: adminKeys.users(filters),
    queryFn: async () => (await api.get<{ items: StaffAccount[] }>('/admin/users', { ...filters, take: 200 })).items,
  })
}

export function useRoleMatrix() {
  return useQuery({ queryKey: adminKeys.roles(), queryFn: () => api.get<RoleMatrix>('/admin/roles'), staleTime: 10 * 60_000 })
}

export function useDevices(filters: DeviceFilters) {
  return useQuery({ queryKey: adminKeys.devices(filters), queryFn: () => api.get<DeviceList>('/admin/devices', { ...filters }), refetchInterval: 30_000 })
}

const refresh = [adminKeys.all, ['audit']]

export function useInviteUser() {
  return useAction({
    mutationFn: (body: { fullName: string; email: string; role: string; stationCodes: string[] }) => api.post<StaffInvite>('/admin/users', body),
    successMessage: (invite) => `Invitation created for ${invite.account.fullName}.`,
    failureMessage: 'Could not invite the person.',
    invalidate: refresh,
  })
}

export function useUpdateUser(id: string) {
  return useAction({
    mutationFn: (body: { fullName?: string; email?: string; role?: string; stationCodes?: string[]; reason?: string }) => api.patch<StaffAccount>(`/admin/users/${id}`, body),
    successMessage: 'Changes saved.',
    failureMessage: 'Could not save the changes.',
    invalidate: refresh,
  })
}

export function useDisableUser(id: string) {
  return useAction({
    mutationFn: (reason: string) => api.post(`/admin/users/${id}/disable`, { reason }),
    successMessage: 'Access disabled. Their sessions have ended.',
    failureMessage: 'Could not disable access.',
    invalidate: refresh,
  })
}

export function useEnableUser(id: string) {
  return useAction({
    mutationFn: () => api.post(`/admin/users/${id}/enable`),
    successMessage: 'Access enabled.',
    failureMessage: 'Could not enable access.',
    invalidate: refresh,
  })
}

export function useUnlockUser(id: string) {
  return useAction({
    mutationFn: (reason: string) => api.post(`/admin/users/${id}/unlock`, { reason }),
    successMessage: 'Account unlocked.',
    failureMessage: 'Could not unlock the account.',
    invalidate: refresh,
  })
}

export function useRegisterDevice() {
  return useAction({
    mutationFn: (body: { name: string; type: string; stationCode: string; location?: string }) => api.post<{ id: string }>('/admin/devices', body),
    successMessage: 'Device registered. It shows Pending until it is first heard from.',
    failureMessage: 'Could not register the device.',
    invalidate: refresh,
  })
}

export function useRevokeDevice(id: string) {
  return useAction({
    mutationFn: (reason: string) => api.post(`/admin/devices/${id}/revoke`, { reason }),
    successMessage: 'Device revoked.',
    failureMessage: 'Could not revoke the device.',
    invalidate: refresh,
  })
}

