import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'

export interface Person {
  subject: string
  name: string
}

/** Active drivers or controllers by name, for pickers (GET /api/ops/people). */
export function usePeople(role: 'driver' | 'controller', enabled = true) {
  return useQuery({
    queryKey: ['people', role],
    queryFn: () => api.get<Person[]>('/ops/people', { role }),
    enabled,
    staleTime: 5 * 60_000,
  })
}
