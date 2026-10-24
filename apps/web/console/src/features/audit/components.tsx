import { EmptyState, StatusPill, Table, Td, Th, THead, Tr } from '@/shared/components/ui'
import { formatDateTime } from '@/shared/utils'
import type { AuditEntry } from './hooks'

export function AuditTable({ entries }: { entries: AuditEntry[] }) {
  if (entries.length === 0) return <EmptyState title="Nothing in the log for these filters" description="Every high-consequence action is recorded here with who did it and why." />

  return (
    <Table caption="Audit log">
      <THead>
        <Th>When</Th>
        <Th>Who</Th>
        <Th>Action</Th>
        <Th>On</Th>
        <Th>Reason</Th>
        <Th>Log</Th>
      </THead>
      <tbody>
        {entries.map((entry) => (
          <Tr key={`${entry.module}-${entry.id}`}>
            <Td className="whitespace-nowrap">{formatDateTime(entry.occurredAtUtc)}</Td>
            <Td>
              <span className="font-semibold">{entry.actorName ?? entry.actorSubject}</span>
              {entry.actorRole && <span className="block text-xs text-fg-tertiary">{entry.actorRole}</span>}
            </Td>
            <Td className="font-semibold">{entry.actionLabel}</Td>
            <Td className="text-fg-secondary">{entry.target ?? entry.targetType}</Td>
            <Td className="max-w-64 truncate text-fg-secondary" title={entry.reason ?? undefined}>{entry.reason ?? '–'}</Td>
            <Td><StatusPill tone={entry.module === 'identity' ? 'changed' : 'neutral'}>{entry.module}</StatusPill></Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
