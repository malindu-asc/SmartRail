import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'

// GET /api/ops/reports/punctuality (guide, section 4.3)

export interface PunctualityReport {
  from: string
  to: string
  journeys: number
  onTimePercent: number | null
  averageDelayMinutes: number
  cancelled: number
  correctedRecords: number
  days: { date: string; journeys: number; onTimePercent: number | null }[]
  lines: { routeCode: string; name: string; journeys: number; onTimePercent: number | null; averageDelayMinutes: number; cancelled: number }[]
}

export interface ReportFilters {
  from: string
  to: string
  route?: string
}

export const reportKeys = { all: ['reports'] as const, punctuality: (filters: ReportFilters) => [...reportKeys.all, 'punctuality', filters] as const }

export function usePunctuality(filters: ReportFilters) {
  return useQuery({ queryKey: reportKeys.punctuality(filters), queryFn: () => api.get<PunctualityReport>('/ops/reports/punctuality', { ...filters }) })
}

/** The same report as a CSV string for download. */
export function fetchPunctualityCsv(filters: ReportFilters): Promise<string> {
  return api.text('/ops/reports/punctuality.csv', { ...filters })
}

