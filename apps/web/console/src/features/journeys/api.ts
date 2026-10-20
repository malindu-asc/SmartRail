import { api } from '@/shared/api'
import type {
  ActionResult,
  Board,
  BoardFilters,
  Impact,
  ImpactAction,
  JourneyDetail,
  JourneyEvent,
  PlatformCheck,
  ReasonCatalog,
  RouteDetail,
  RouteSummary,
} from './types'

export const journeysApi = {
  board: (filters: BoardFilters) => api.get<Board>('/ops/journeys', { ...filters }),
  detail: (id: string) => api.get<JourneyDetail>(`/ops/journeys/${id}`),
  events: (id: string) => api.get<JourneyEvent[]>(`/ops/journeys/${id}/events`, { take: 30 }),
  reasonCodes: () => api.get<ReasonCatalog>('/ops/reason-codes'),
  impact: (id: string, action: ImpactAction, stopSequence?: number) => api.get<Impact>(`/ops/journeys/${id}/impact`, { action, stopSequence }),
  platformCheck: (id: string, stopSequence: number, platform: string) =>
    api.get<PlatformCheck>(`/ops/journeys/${id}/platform-check`, { stopSequence, platform }),
  routes: (date?: string) => api.get<RouteSummary[]>('/ops/routes', { date }),
  route: (key: string, date?: string) => api.get<RouteDetail>(`/ops/routes/${key}`, { date }),

  cancel: (id: string, body: { reasonCode: string; note?: string; passengerMessage?: string; expectedVersion: number }) =>
    api.post<ActionResult>(`/ops/journeys/${id}/cancel`, body),
  terminateShort: (id: string, body: { stopSequence: number; reasonCode: string; note?: string; expectedVersion: number }) =>
    api.post<ActionResult>(`/ops/journeys/${id}/terminate-short`, body),
  interrupt: (id: string, body: { reasonCode: string; note?: string; expectedVersion: number }) =>
    api.post<ActionResult>(`/ops/journeys/${id}/interrupt`, body),
  resume: (id: string, body: { expectedVersion: number }) => api.post<ActionResult>(`/ops/journeys/${id}/resume`, body),
  reinstate: (id: string, body: { reason: string; expectedVersion: number }) => api.post<ActionResult>(`/ops/journeys/${id}/reinstate`, body),
  changePlatform: (
    id: string,
    body: { stopSequence: number; platform: string; reasonCode: string; note?: string; confirmOverlap: boolean; expectedVersion: number },
  ) => api.post<ActionResult>(`/ops/journeys/${id}/platform`, body),
}
