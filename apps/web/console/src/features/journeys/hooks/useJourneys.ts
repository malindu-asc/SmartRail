import { queryOptions, useQuery } from '@tanstack/react-query'
import { useAction } from '@/shared/hooks'
import { journeysApi } from '../api'
import type { BoardFilters, ImpactAction } from '../types'

/** Query-key factory: one place that says what is cached under which key. */
export const journeyKeys = {
  all: ['journeys'] as const,
  boards: () => [...journeyKeys.all, 'board'] as const,
  board: (filters: BoardFilters) => [...journeyKeys.boards(), filters] as const,
  details: () => [...journeyKeys.all, 'detail'] as const,
  detail: (id: string) => [...journeyKeys.details(), id] as const,
  events: (id: string) => [...journeyKeys.all, 'events', id] as const,
  reasons: () => [...journeyKeys.all, 'reasons'] as const,
  impact: (id: string, action: ImpactAction, stop?: number) => [...journeyKeys.all, 'impact', id, action, stop ?? 0] as const,
  platformCheck: (id: string, stop: number, platform: string) => [...journeyKeys.all, 'platform-check', id, stop, platform] as const,
  routes: (date?: string) => [...journeyKeys.all, 'routes', date ?? 'today'] as const,
  route: (key: string, date?: string) => [...journeyKeys.all, 'route', key, date ?? 'today'] as const,
}

/** The live board refreshes itself every 15 seconds. */
export const BOARD_REFRESH_MS = 15_000

export function boardQueryOptions(filters: BoardFilters) {
  return queryOptions({ queryKey: journeyKeys.board(filters), queryFn: () => journeysApi.board(filters), refetchInterval: BOARD_REFRESH_MS })
}

export function useBoard(filters: BoardFilters) {
  return useQuery(boardQueryOptions(filters))
}

export function useJourney(id: string) {
  return useQuery({ queryKey: journeyKeys.detail(id), queryFn: () => journeysApi.detail(id), refetchInterval: BOARD_REFRESH_MS })
}

export function useJourneyEvents(id: string) {
  return useQuery({ queryKey: journeyKeys.events(id), queryFn: () => journeysApi.events(id), refetchInterval: 30_000 })
}

export function useReasonCodes() {
  return useQuery({ queryKey: journeyKeys.reasons(), queryFn: journeysApi.reasonCodes, staleTime: 10 * 60_000 })
}

/** Who a cancel, short termination or platform change reaches. Fetched when the confirmation modal opens. */
export function useImpact(id: string, action: ImpactAction, stopSequence: number | undefined, enabled: boolean) {
  return useQuery({
    queryKey: journeyKeys.impact(id, action, stopSequence),
    queryFn: () => journeysApi.impact(id, action, stopSequence),
    enabled,
    staleTime: 0,
  })
}

export function usePlatformCheck(id: string, stopSequence: number, platform: string, enabled: boolean) {
  return useQuery({
    queryKey: journeyKeys.platformCheck(id, stopSequence, platform),
    queryFn: () => journeysApi.platformCheck(id, stopSequence, platform),
    enabled: enabled && platform.trim().length > 0,
    staleTime: 0,
  })
}

export function useRoutes(date?: string) {
  return useQuery({ queryKey: journeyKeys.routes(date), queryFn: () => journeysApi.routes(date), refetchInterval: BOARD_REFRESH_MS })
}

export function useRoute(key: string, date?: string) {
  return useQuery({ queryKey: journeyKeys.route(key, date), queryFn: () => journeysApi.route(key, date) })
}

const refresh = [journeyKeys.all]

export function useCancelJourney(id: string) {
  return useAction({
    mutationFn: (body: Parameters<typeof journeysApi.cancel>[1]) => journeysApi.cancel(id, body),
    successMessage: 'Journey cancelled. Its tickets are now marked Affected.',
    failureMessage: 'Could not cancel the journey.',
    invalidate: refresh,
  })
}

export function useTerminateShort(id: string) {
  return useAction({
    mutationFn: (body: Parameters<typeof journeysApi.terminateShort>[1]) => journeysApi.terminateShort(id, body),
    successMessage: 'Journey terminated short.',
    failureMessage: 'Could not terminate the journey.',
    invalidate: refresh,
  })
}

export function useInterrupt(id: string) {
  return useAction({
    mutationFn: (body: Parameters<typeof journeysApi.interrupt>[1]) => journeysApi.interrupt(id, body),
    successMessage: 'Journey interrupted.',
    failureMessage: 'Could not interrupt the journey.',
    invalidate: refresh,
  })
}

export function useResume(id: string) {
  return useAction({
    mutationFn: (body: Parameters<typeof journeysApi.resume>[1]) => journeysApi.resume(id, body),
    successMessage: 'Journey resumed.',
    failureMessage: 'Could not resume the journey.',
    invalidate: refresh,
  })
}

export function useReinstate(id: string) {
  return useAction({
    mutationFn: (body: Parameters<typeof journeysApi.reinstate>[1]) => journeysApi.reinstate(id, body),
    successMessage: 'Journey reinstated.',
    failureMessage: 'Could not reinstate the journey.',
    invalidate: refresh,
  })
}

export function useChangePlatform(id: string) {
  return useAction({
    mutationFn: (body: Parameters<typeof journeysApi.changePlatform>[1]) => journeysApi.changePlatform(id, body),
    successMessage: 'Platform changed. Displays and passengers will show it.',
    failureMessage: 'Could not change the platform.',
    invalidate: refresh,
  })
}
