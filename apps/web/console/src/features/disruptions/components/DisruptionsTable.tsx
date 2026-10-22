import { Link } from 'react-router-dom'
import { EmptyState, StatusPill, Table, Td, Th, THead, Tr, toneFor } from '@/shared/components/ui'
import { formatDateTime, humanize } from '@/shared/utils'
import type { DisruptionRow } from '../types'

export function StatusLabel({ row }: { row: Pick<DisruptionRow, 'status' | 'reviewExpired' | 'planned'> }) {
  if (row.reviewExpired) return <StatusPill tone="cancelled">Expired, review</StatusPill>
  if (row.planned) return <StatusPill tone="changed">Planned</StatusPill>
  return <StatusPill tone={toneFor(row.status)}>{humanize(row.status)}</StatusPill>
}

/** `linkRows` is false for people who cannot open a notice (a scheduler sees planned works as a list). */
export function DisruptionsTable({ items, linkRows = true }: { items: DisruptionRow[]; linkRows?: boolean }) {
  if (items.length === 0) return <EmptyState title="Nothing here" description="Notices in this list will appear as they are created." />

  return (
    <Table caption="Disruptions">
      <THead>
        <Th>Notice</Th>
        <Th>Where</Th>
        <Th>Severity</Th>
        <Th>Status</Th>
        <Th>Review by</Th>
        <Th>Written by</Th>
      </THead>
      <tbody>
        {items.map((row) => (
          <Tr key={row.id} tone={row.status === 'AwaitingApproval' ? 'attention' : row.reviewExpired ? 'danger' : undefined}>
            <Td>
              {linkRows ? <Link to={`/disruptions/${row.id}`} className="font-semibold hover:underline">{row.title}</Link> : <span className="font-semibold">{row.title}</span>}
              {row.type && <span className="block text-xs text-fg-tertiary">{row.type}</span>}
            </Td>
            <Td className="max-w-56 truncate text-fg-secondary">{row.scope}</Td>
            <Td><StatusPill tone={toneFor(row.severity)}>{row.severity}</StatusPill></Td>
            <Td><StatusLabel row={row} /></Td>
            <Td className="whitespace-nowrap text-fg-secondary">{formatDateTime(row.reviewByUtc)}</Td>
            <Td>{row.ownerName ?? 'Unknown'}</Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
