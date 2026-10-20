import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'
import { useAction } from '@/shared/hooks'
import type { DisruptionBody, DisruptionDetail, DisruptionList, DisruptionPreview, DisruptionTab } from '../types'

export const disruptionKeys = {
  all: ['disruptions'] as const,
  list: (tab: DisruptionTab) => [...disruptionKeys.all, 'list', tab] as const,
  detail: (id: string) => [...disruptionKeys.all, 'detail', id] as const,
}

/** The console list for one tab, with the counts for all tabs. A scheduler's planned works use the planned-works endpoint. */
export function useDisruptionList(tab: DisruptionTab, planner = false) {
  return useQuery({
    queryKey: [...disruptionKeys.list(tab), planner],
    queryFn: () => (planner ? api.get<DisruptionList>('/ops/planned-works') : api.get<DisruptionList>('/ops/disruptions', { tab })),
    refetchInterval: 20_000,
  })
}

export function useDisruption(id: string) {
  return useQuery({ queryKey: disruptionKeys.detail(id), queryFn: () => api.get<DisruptionDetail>(`/ops/disruptions/${id}`) })
}

/** How the notice will look to passengers; asked as the person types. */
export function usePreviewDisruption() {
  return useMutation({
    mutationFn: (body: { title: string; message: string; severity: string }) => api.post<DisruptionPreview>('/ops/disruptions/preview', body),
  })
}

// A notice is also on the dashboard, in incidents and in the public alerts, so those caches are refreshed too.
const refresh = [disruptionKeys.all, ['dashboard'], ['incidents']]

export function useSaveDisruption(options: { id?: string; planned: boolean }) {
  return useAction({
    mutationFn: (body: DisruptionBody) => {
      if (options.planned) return api.post<DisruptionDetail>('/ops/planned-works', body)
      return options.id ? api.put<DisruptionDetail>(`/ops/disruptions/${options.id}`, body) : api.post<DisruptionDetail>('/ops/disruptions', body)
    },
    successMessage: (detail, body) => {
      if (!body.submit) return 'Draft saved.'
      return detail.status === 'AwaitingApproval' ? 'Sent for approval. A different controller must approve it before passengers see it.' : 'Published. Passengers see it now.'
    },
    failureMessage: 'The notice was not saved.',
    invalidate: refresh,
  })
}

export function useSubmitDisruption(id: string) {
  return useAction({
    mutationFn: () => api.post(`/ops/disruptions/${id}/submit`),
    successMessage: 'Sent on its way.',
    failureMessage: 'Could not send the notice.',
    invalidate: refresh,
  })
}

export function useApproveDisruption(id: string) {
  return useAction({
    mutationFn: () => api.post(`/ops/disruptions/${id}/approve`),
    successMessage: 'Approved. Passengers see the notice now.',
    failureMessage: 'Could not approve the notice.',
    invalidate: refresh,
  })
}

export function useRejectDisruption(id: string) {
  return useAction({
    mutationFn: (comment: string) => api.post(`/ops/disruptions/${id}/reject`, { comment }),
    successMessage: 'Sent back to its author with your comment.',
    failureMessage: 'Could not reject the notice.',
    invalidate: refresh,
  })
}

export function useResolveDisruption(id: string) {
  return useAction({
    mutationFn: (reason?: string) => api.post(`/ops/disruptions/${id}/resolve`, { reason }),
    successMessage: 'Marked as over.',
    failureMessage: 'Could not resolve the notice.',
    invalidate: refresh,
  })
}

export function useRetractDisruption(id: string) {
  return useAction({
    mutationFn: (reason: string) => api.post(`/ops/disruptions/${id}/retract`, { reason }),
    successMessage: 'Retracted. It disappears from passenger apps within seconds.',
    failureMessage: 'Could not retract the notice.',
    invalidate: refresh,
  })
}
