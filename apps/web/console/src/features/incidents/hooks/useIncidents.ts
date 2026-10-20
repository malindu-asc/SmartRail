import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'
import { useAction } from '@/shared/hooks'
import type { DeclareBody, Incident, IncidentDetail, SignOutCheck } from '../types'

export const incidentKeys = {
  all: ['incidents'] as const,
  list: (openOnly: boolean) => [...incidentKeys.all, 'list', openOnly] as const,
  detail: (id: string) => [...incidentKeys.all, 'detail', id] as const,
  signOut: () => [...incidentKeys.all, 'sign-out-check'] as const,
}

export function useIncidents(openOnly: boolean) {
  return useQuery({ queryKey: incidentKeys.list(openOnly), queryFn: () => api.get<Incident[]>('/ops/incidents', { openOnly }), refetchInterval: 20_000 })
}

export function useIncident(id: string) {
  return useQuery({ queryKey: incidentKeys.detail(id), queryFn: () => api.get<IncidentDetail>(`/ops/incidents/${id}`), refetchInterval: 20_000 })
}

/** What the signed-in controller still owns. Only asked for people who may own incidents. */
export function useSignOutCheck(enabled: boolean) {
  return useQuery({ queryKey: incidentKeys.signOut(), queryFn: () => api.get<SignOutCheck>('/ops/incidents/sign-out-check'), enabled, staleTime: 0 })
}

const refresh = [incidentKeys.all, ['dashboard']]

export function useDeclareIncident() {
  return useAction({
    mutationFn: (body: DeclareBody) => api.post<IncidentDetail>('/ops/incidents', body),
    successMessage: 'Incident declared. You are its owner.',
    failureMessage: 'Could not declare the incident.',
    invalidate: refresh,
  })
}

export function useAddIncidentNote(id: string) {
  return useAction({
    mutationFn: (body: { kind: 'Note' | 'Action'; text: string; detail?: string }) => api.post(`/ops/incidents/${id}/notes`, body),
    successMessage: 'Added to the timeline.',
    failureMessage: 'Could not add to the timeline.',
    invalidate: refresh,
  })
}

export function useLinkDisruption(id: string) {
  return useAction({
    mutationFn: (disruptionId: string) => api.post(`/ops/incidents/${id}/link-disruption`, { disruptionId }),
    successMessage: 'Passenger notice linked.',
    failureMessage: 'Could not link the notice.',
    invalidate: refresh,
  })
}

export function useHandOverIncident(id: string) {
  return useAction({
    mutationFn: (body: { newOwnerSubject: string; note: string }) => api.post(`/ops/incidents/${id}/handover`, body),
    successMessage: 'Incident handed over.',
    failureMessage: 'Could not hand the incident over.',
    invalidate: refresh,
  })
}

export function useResolveIncident(id: string) {
  return useAction({
    mutationFn: (note: string) => api.post(`/ops/incidents/${id}/resolve`, { note }),
    successMessage: 'Incident resolved.',
    failureMessage: 'Could not resolve the incident.',
    invalidate: refresh,
  })
}

export function useEmergencyTermination() {
  return useAction({
    mutationFn: (vars: { incidentId: string; journeyId: string; reason: string }) =>
      api.post(`/ops/incidents/${vars.incidentId}/emergency-termination`, { journeyId: vars.journeyId, confirm: true, reason: vars.reason }),
    successMessage: 'Train terminated. Passengers see the approved emergency wording.',
    failureMessage: 'Could not terminate the train.',
    invalidate: [...refresh, ['journeys']],
  })
}
