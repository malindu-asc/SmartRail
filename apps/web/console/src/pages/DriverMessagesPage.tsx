import { Send } from 'lucide-react'
import { useState } from 'react'
import { AssistanceList, MessagesTable, SendMessageModal, useAssistance, useMessages, useResendMessage, type Message } from '@/features/messages'
import { usePeople } from '@/features/people'
import { ConfirmModal, ErrorState, ListSkeleton, PageHeader, TableSkeleton, Tabs, Button } from '@/shared/components/ui'
import { useQueryParam } from '@/shared/hooks'

/** Messages to drivers and their acknowledgements, and drivers asking for help. */
export function DriverMessagesPage() {
  const [tab, setTab] = useQueryParam('tab', 'messages')
  const [sending, setSending] = useState(false)
  const [resending, setResending] = useState<Message | null>(null)
  const messages = useMessages(false)
  const assistance = useAssistance()
  const drivers = usePeople('driver', sending)
  const resend = useResendMessage()

  return (
    <>
      <PageHeader
        title="Driver messages"
        description="Plain-text messages to drivers. Acknowledgements show here; one that is not acknowledged is flagged."
        actions={<Button icon={<Send aria-hidden className="size-4" />} onClick={() => setSending(true)}>New message</Button>}
      />
      <Tabs
        label="Messages and assistance"
        value={tab}
        onChange={setTab}
        items={[
          { value: 'messages', label: 'Messages', count: messages.data?.open },
          { value: 'assistance', label: 'Driver reports', count: assistance.data?.length },
        ]}
      />

      {tab === 'assistance' ? (
        assistance.isPending ? <ListSkeleton label="Loading requests" /> : assistance.isError ? <ErrorState error={assistance.error} onRetry={() => void assistance.refetch()} /> : <AssistanceList items={assistance.data} />
      ) : messages.isPending ? (
        <TableSkeleton columns={5} label="Loading messages" />
      ) : messages.isError ? (
        <ErrorState error={messages.error} onRetry={() => void messages.refetch()} />
      ) : (
        <MessagesTable messages={messages.data.items} onResend={setResending} />
      )}

      {sending && <SendMessageModal open onClose={() => setSending(false)} drivers={(drivers.data ?? []).map((d) => ({ subject: d.subject, name: d.name }))} />}
      <ConfirmModal
        open={resending !== null}
        onClose={() => setResending(null)}
        title="Send this message again"
        description={resending ? `${resending.driverName ?? 'The driver'} will get it again and it stays valid for 30 minutes.` : undefined}
        confirmLabel="Send again"
        pending={resend.isPending}
        onConfirm={() => resending && resend.mutate({ id: resending.id, validForMinutes: 30 }, { onSuccess: () => setResending(null) })}
      />
    </>
  )
}
