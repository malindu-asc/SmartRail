import { Button, EmptyState, StatusPill, Table, Td, Th, THead, Tr } from '@/shared/components/ui'
import { Can, Permissions } from '@/shared/auth'
import { formatTime } from '@/shared/utils'
import type { Message } from '../types'

interface Props {
  messages: Message[]
  onResend: (message: Message) => void
}

function StateCell({ message }: { message: Message }) {
  if (message.overdue) return <StatusPill tone="cancelled">Not acknowledged · {message.overdueMinutes ?? 0} min</StatusPill>
  if (message.expired && message.state !== 'Acknowledged') return <StatusPill tone="stale">Expired</StatusPill>
  const tone = message.state === 'Acknowledged' ? 'ontime' : message.state === 'Delivered' ? 'changed' : 'delayed'
  return <StatusPill tone={tone}>{message.state}</StatusPill>
}

/** Today's messages to drivers with whether the driver has seen and acknowledged each one. */
export function MessagesTable({ messages, onResend }: Props) {
  if (messages.length === 0) return <EmptyState title="No messages today" description="Messages you send to drivers appear here with their state." />

  return (
    <Table caption="Messages to drivers">
      <THead>
        <Th>Sent</Th>
        <Th>To</Th>
        <Th>Message</Th>
        <Th>State</Th>
        <Th><span className="sr-only">Actions</span></Th>
      </THead>
      <tbody>
        {messages.map((message) => (
          <Tr key={message.id} tone={message.overdue ? 'danger' : undefined}>
            <Td className="whitespace-nowrap font-semibold">{formatTime(message.sentAtUtc)}</Td>
            <Td>
              {message.driverName ?? 'Driver'}
              <span className="block text-xs text-fg-tertiary">{[message.trainNumber && `Train ${message.trainNumber}`, message.unitCode && `Unit ${message.unitCode}`].filter(Boolean).join(' · ')}</span>
            </Td>
            <Td className="max-w-md">
              <p className="line-clamp-2">{message.body}</p>
              {message.resentCount > 0 && <span className="text-xs text-fg-tertiary">Sent {message.resentCount + 1} times</span>}
            </Td>
            <Td><StateCell message={message} /></Td>
            <Td className="text-right">
              <Can permission={Permissions.MessageSendToDriver}>
                {message.state !== 'Acknowledged' && !message.expired && <Button size="sm" variant="secondary" onClick={() => onResend(message)}>Send again</Button>}
              </Can>
            </Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
