import { Card, CardHeader, EmptyState, StatCard, Table, Td, Th, THead, Tr } from '@/shared/components/ui'
import { cn, formatDate } from '@/shared/utils'
import type { PunctualityReport } from './hooks'

const percent = (value: number | null) => (value === null ? '–' : `${value}%`)

export function PunctualitySummary({ report }: { report: PunctualityReport }) {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      <StatCard label="On time" value={percent(report.onTimePercent)} tone="ontime" note={`${report.journeys} journeys`} />
      <StatCard label="Average delay" value={`${report.averageDelayMinutes.toFixed(1)} min`} />
      <StatCard label="Cancelled" value={report.cancelled} tone="cancelled" />
      <StatCard label="Corrected records" value={report.correctedRecords} note="controller or superseded events" />
    </div>
  )
}

/** One bar per day. The bar is the on-time percentage; a day with no data is empty, never shown as on time. */
export function PunctualityDays({ days }: { days: PunctualityReport['days'] }) {
  if (days.length === 0) return <EmptyState title="No journeys in this period" />
  return (
    <Card>
      <CardHeader title="On time by day" />
      <ul className="space-y-2 px-5 py-4">
        {days.map((day) => (
          <li key={day.date} className="grid grid-cols-[4rem_1fr_3rem] items-center gap-3">
            <span className="text-fg-secondary">{formatDate(day.date)}</span>
            <div role="img" aria-label={`${formatDate(day.date)}: ${percent(day.onTimePercent)} on time, ${day.journeys} journeys`} className="h-2 rounded-full bg-soft">
              <div className={cn('h-full rounded-full', (day.onTimePercent ?? 0) >= 85 ? 'bg-on-time' : 'bg-delayed')} style={{ width: `${day.onTimePercent ?? 0}%` }} />
            </div>
            <span className="text-right font-semibold">{percent(day.onTimePercent)}</span>
          </li>
        ))}
      </ul>
    </Card>
  )
}

export function PunctualityLines({ lines }: { lines: PunctualityReport['lines'] }) {
  if (lines.length === 0) return null
  return (
    <Table caption="Punctuality by route">
      <THead>
        <Th>Route</Th>
        <Th>Journeys</Th>
        <Th>On time</Th>
        <Th>Average delay</Th>
        <Th>Cancelled</Th>
      </THead>
      <tbody>
        {lines.map((line) => (
          <Tr key={line.routeCode}>
            <Td className="font-semibold">{line.name} <span className="text-xs font-normal text-fg-tertiary">{line.routeCode}</span></Td>
            <Td>{line.journeys}</Td>
            <Td>{percent(line.onTimePercent)}</Td>
            <Td>{line.averageDelayMinutes.toFixed(1)} min</Td>
            <Td>{line.cancelled}</Td>
          </Tr>
        ))}
      </tbody>
    </Table>
  )
}
