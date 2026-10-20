import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'
import { useAction } from '@/shared/hooks'
import type { AddServiceBody, Comparison, StopTimeInput, Version, VersionDetail } from '../types'

export const timetableKeys = {
  all: ['timetable'] as const,
  list: () => [...timetableKeys.all, 'list'] as const,
  detail: (id: string) => [...timetableKeys.all, 'detail', id] as const,
  compare: (id: string) => [...timetableKeys.all, 'compare', id] as const,
}

export function useVersions() {
  return useQuery({ queryKey: timetableKeys.list(), queryFn: async () => (await api.get<{ items: Version[] }>('/ops/timetable/versions')).items })
}

export function useVersion(id: string) {
  return useQuery({ queryKey: timetableKeys.detail(id), queryFn: () => api.get<VersionDetail>(`/ops/timetable/versions/${id}`) })
}

export function useComparison(id: string, enabled: boolean) {
  return useQuery({ queryKey: timetableKeys.compare(id), queryFn: () => api.get<Comparison>(`/ops/timetable/versions/${id}/compare`), enabled, staleTime: 0 })
}

const refresh = [timetableKeys.all]

export function useCreateDraft() {
  return useAction({
    mutationFn: (label: string) => api.post<VersionDetail>('/ops/timetable/versions', { label }),
    successMessage: 'Draft created as a copy of the timetable in force.',
    failureMessage: 'Could not create the draft.',
    invalidate: refresh,
  })
}

export function useChangeTimes(versionId: string) {
  return useAction({
    mutationFn: (vars: { serviceId: string; stops: StopTimeInput[] }) =>
      api.put(`/ops/timetable/versions/${versionId}/services/${vars.serviceId}/times`, { stops: vars.stops }),
    successMessage: 'Times changed.',
    failureMessage: 'Could not change the times.',
    invalidate: refresh,
  })
}

export function useAddService(versionId: string) {
  return useAction({
    mutationFn: (body: AddServiceBody) => api.post<{ id: string }>(`/ops/timetable/versions/${versionId}/services`, body),
    successMessage: (_, body) => `Service ${body.trainNumber} added.`,
    failureMessage: 'Could not add the service.',
    invalidate: refresh,
  })
}

export function useRemoveService(versionId: string) {
  return useAction({
    mutationFn: (serviceId: string) => api.delete(`/ops/timetable/versions/${versionId}/services/${serviceId}`),
    successMessage: 'Service removed from the draft.',
    failureMessage: 'Could not remove the service.',
    invalidate: refresh,
  })
}

export function useSubmitVersion(versionId: string) {
  return useAction({
    mutationFn: () => api.post(`/ops/timetable/versions/${versionId}/submit`),
    successMessage: 'Sent for review. A different scheduler must approve it.',
    failureMessage: 'Could not send the draft for review.',
    invalidate: refresh,
  })
}

export function useApproveVersion(versionId: string) {
  return useAction({
    mutationFn: (effectiveFrom: string) => api.post(`/ops/timetable/versions/${versionId}/approve`, { effectiveFrom }),
    successMessage: (_, date) => `Approved. It takes effect on ${date}; journeys already generated are not changed.`,
    failureMessage: 'Could not approve the timetable.',
    invalidate: [...refresh, ['journeys']],
  })
}
