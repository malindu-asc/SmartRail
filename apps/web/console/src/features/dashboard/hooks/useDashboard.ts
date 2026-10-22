import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'
import type { Dashboard } from '../types'

export const dashboardKeys = { all: ['dashboard'] as const }

/** Refreshes every 15 seconds: the controller keeps this open all shift. */
export function useDashboard() {
  return useQuery({ queryKey: dashboardKeys.all, queryFn: () => api.get<Dashboard>('/ops/dashboard'), refetchInterval: 15_000 })
}
