import { useQuery } from '@tanstack/react-query'
import { api } from '@/shared/api'
import { useAction } from '@/shared/hooks'
import type { Assistance, Message, MessageList, MessageTemplate, SendMessageBody } from '../types'

export const messageKeys = {
  all: ['messages'] as const,
  list: (openOnly: boolean) => [...messageKeys.all, 'list', openOnly] as const,
  templates: () => [...messageKeys.all, 'templates'] as const,
  assistance: () => [...messageKeys.all, 'assistance'] as const,
}

/** Messages update as drivers read and acknowledge them, so the list refreshes often. */
export function useMessages(openOnly: boolean) {
  return useQuery({ queryKey: messageKeys.list(openOnly), queryFn: () => api.get<MessageList>('/ops/messages', { openOnly }), refetchInterval: 10_000 })
}

export function useMessageTemplates() {
  return useQuery({ queryKey: messageKeys.templates(), queryFn: () => api.get<MessageTemplate[]>('/ops/message-templates'), staleTime: 10 * 60_000 })
}

export function useAssistance() {
  return useQuery({ queryKey: messageKeys.assistance(), queryFn: () => api.get<Assistance[]>('/ops/assistance'), refetchInterval: 10_000 })
}

// A message or a help request also shows on the dashboard, so both are refreshed.
const refresh = [messageKeys.all, ['dashboard']]

export function useSendMessage() {
  return useAction({
    mutationFn: (body: SendMessageBody) => api.post<Message>('/ops/messages', body),
    successMessage: (message) => `Message sent to ${message.driverName ?? 'the driver'}.`,
    failureMessage: 'The message was not sent.',
    invalidate: refresh,
  })
}

export function useResendMessage() {
  return useAction({
    mutationFn: (vars: { id: string; validForMinutes: number }) => api.post<Message>(`/ops/messages/${vars.id}/resend`, { validForMinutes: vars.validForMinutes }),
    successMessage: 'Message sent again.',
    failureMessage: 'Could not resend the message.',
    invalidate: refresh,
  })
}

export function useHandleAssistance() {
  return useAction({
    mutationFn: (vars: { id: string; outcome?: string }) => api.post(`/ops/assistance/${vars.id}/handle`, { outcome: vars.outcome }),
    successMessage: 'Marked as handled.',
    failureMessage: 'Could not update the request.',
    invalidate: refresh,
  })
}
