import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'
import { useAction } from '@/shared/hooks'
import type { AssignmentOptions, CrewRow, PlanningNeeds, SuggestionOutcome, UnitAssignmentRow, UnitImpact, UnitRow } from '../types'

export const planningKeys = {
  all: ['planning'] as const,
  needs: (date?: string) => [...planningKeys.all, 'needs', date ?? 'today'] as const,
  assignments: (date?: string) => [...planningKeys.all, 'assignments', date ?? 'today'] as const,
  units: (date?: string) => [...planningKeys.all, 'units', date ?? 'today'] as const,
  crew: (date?: string) => [...planningKeys.all, 'crew', date ?? 'today'] as const,
  options: (journeyId: string) => [...planningKeys.all, 'options', journeyId] as const,
  impact: (unitId: string) => [...planningKeys.all, 'impact', unitId] as const,
}

export function usePlanningNeeds(date?: string) {
  return useQuery({ queryKey: planningKeys.needs(date), queryFn: () => api.get<PlanningNeeds>('/ops/planning/needs', { date }), refetchInterval: 20_000 })
}

export function useAssignments(date?: string) {
  return useQuery({ queryKey: planningKeys.assignments(date), queryFn: () => api.get<UnitAssignmentRow[]>('/ops/planning/assignments', { date }) })
}

export function useUnits(date?: string) {
  return useQuery({ queryKey: planningKeys.units(date), queryFn: () => api.get<UnitRow[]>('/ops/units', { date }) })
}

export function useCrew(date?: string) {
  return useQuery({ queryKey: planningKeys.crew(date), queryFn: () => api.get<CrewRow[]>('/ops/crew', { date }) })
}

export function useAssignmentOptions(journeyId: string, enabled: boolean) {
  return useQuery({
    queryKey: planningKeys.options(journeyId),
    queryFn: () => api.get<AssignmentOptions>(`/ops/journeys/${journeyId}/assignment-options`),
    enabled,
    staleTime: 0,
  })
}

export function useUnitImpact(unitId: string, enabled: boolean) {
  return useQuery({ queryKey: planningKeys.impact(unitId), queryFn: () => api.get<UnitImpact>(`/ops/units/${unitId}/impact`), enabled, staleTime: 0 })
}

// Planning changes move journeys, the board and the dashboard's "needs attention", so all of them are refreshed.
const refresh = [planningKeys.all, ['journeys'], ['dashboard']]

export function useAssign(journeyId: string) {
  return useAction({
    mutationFn: (body: { unitId?: string; dutyId?: string; reason: string; acceptRestConflict: boolean; expectedVersion: number }) =>
      api.post(`/ops/journeys/${journeyId}/assign`, body),
    successMessage: 'Assignment saved.',
    failureMessage: 'Could not assign.',
    invalidate: refresh,
  })
}

export function useUnassign(journeyId: string) {
  return useAction({
    mutationFn: (body: { unit: boolean; driver: boolean; reason: string; expectedVersion: number }) => api.post(`/ops/journeys/${journeyId}/unassign`, body),
    successMessage: 'Removed from the journey.',
    failureMessage: 'Could not remove the assignment.',
    invalidate: refresh,
  })
}

export function useCreateDuty() {
  return useAction({
    mutationFn: (body: { driverSubject: string; fromUtc: string; toUtc: string }) => api.post('/ops/duties', body),
    successMessage: 'Driver rostered.',
    failureMessage: 'Could not roster the driver.',
    invalidate: refresh,
  })
}

export function useMarkOutOfService(unitId: string) {
  return useAction({
    mutationFn: (body: { fromUtc: string; toUtc: string; reason: string }) => api.post<UnitImpact>(`/ops/units/${unitId}/out-of-service`, body),
    successMessage: (impact) => (impact.uncovered > 0 ? `Unit marked out of service. ${impact.uncovered} journeys need another unit.` : 'Unit marked out of service.'),
    failureMessage: 'Could not mark the unit out of service.',
    invalidate: refresh,
  })
}

export function useBackInService(unitId: string) {
  return useAction({
    mutationFn: (periodId: string) => api.delete(`/ops/units/${unitId}/out-of-service/${periodId}`),
    successMessage: 'Unit is back in service.',
    failureMessage: 'Could not update the unit.',
    invalidate: refresh,
  })
}

export function useApplySuggestions(unitId: string) {
  return useAction({
    mutationFn: (body: { assignments?: { journeyId: string; unitId: string }[]; reason: string }) =>
      api.post<SuggestionOutcome[]>(`/ops/units/${unitId}/apply-suggestions`, body),
    successMessage: (outcomes) => {
      const done = outcomes.filter((o) => o.applied).length
      return `${done} of ${outcomes.length} journeys reassigned.`
    },
    failureMessage: 'Could not apply the suggestions.',
    invalidate: refresh,
  })
}
