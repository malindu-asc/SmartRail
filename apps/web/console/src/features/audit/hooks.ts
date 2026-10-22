import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'

// GET /api/audit (guide, section 4.8). Append-only: nothing here can be changed.

export interface AuditEntry {
  id: number
  module: string
  occurredAtUtc: string
  actorSubject: string
  actorName: string | null
  actorRole: string | null
  action: string
  actionLabel: string
  targetType: string
  targetId: string | null
  target: string | null
  reason: string | null
}

export interface AuditFilters {
  kind?: 'operational' | 'access'
  q?: string
  action?: string
  from?: string
  to?: string
}

export const auditKeys = { all: ['audit'] as const, list: (filters: AuditFilters) => [...auditKeys.all, filters] as const }

export function useAudit(filters: AuditFilters) {
  return useQuery({ queryKey: auditKeys.list(filters), queryFn: () => api.get<AuditEntry[]>('/audit', { ...filters, take: 200 }), refetchInterval: 30_000 })
}

