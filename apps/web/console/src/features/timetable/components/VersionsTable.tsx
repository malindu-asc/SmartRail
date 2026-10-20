import { Link } from 'react-router-dom'
import { EmptyState, StatusPill, Table, Td, Th, THead, Tr } from '@/shared/components/ui'
import { formatDate, humanize } from '@/shared/utils'
import type { Version, VersionStatus } from '../types'

const TONE = { Draft: 'delayed', InReview: 'changed', Published: 'ontime', Superseded: 'stale' } as const

export function VersionStatusPill({ version }: { version: Pick<Version, 'status' | 'inForce'> }) {
  const status: VersionStatus = version.status
  return <StatusPill tone={TONE[status]}>{version.inForce ? 'In force' : humanize(status)}</StatusPill>
}

export function VersionsTable({ versions }: { versions: Version[] }) {
  if (versions.length === 0) return <EmptyState title="No timetable versions" description="Versions appear here once the first timetable is published." />

  return (
    <Table caption="Timetable versions">
      <THead>
        <Th>Version</Th>
        <Th>Status</Th>
        <Th>Effective from</Th>
        <Th>Services</Th>
        <Th>Author</Th>
        <Th>Approved by</Th>
      </THead>
      <tbody>
        {versions.map((version) => (
          <Tr key={version.id} tone={version.inForce ? 'good' : undefined}>
            <Td><Link to={`/timetables/${version.id}`} className="font-semibold hover:underline">{version.label}</Link></Td>
            <Td><VersionStatusPill version={version} /></Td>
            <Td>{version.effectiveFrom ? formatDate(version.effectiveFrom) : '–'}</Td>
            <Td>{version.services}</Td>
            <Td>{version.authorName ?? 'Unknown'}</Td>
            <Td>{version.approvedByName ?? '–'}</Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
