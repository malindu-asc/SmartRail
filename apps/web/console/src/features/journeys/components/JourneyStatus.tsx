import { StatusPill, toneFor } from '@/shared/components/ui'
import { cn, formatAge } from '@/shared/utils'
import type { DataFreshness, JourneyRow } from '../types'

/** The coloured chip from the server's status words ("Delayed", "Platform changed"). */
export function JourneyStatus({ journey }: { journey: Pick<JourneyRow, 'statusKind' | 'statusLabel'> }) {
  return <StatusPill tone={toneFor(journey.statusKind)}>{journey.statusLabel}</StatusPill>
}

const DATA_STYLE: Record<DataFreshness, string> = {
  Live: 'text-on-time font-semibold',
  Stale: 'text-delayed font-semibold',
  Unknown: 'text-cancelled font-semibold',
  Scheduled: 'text-fg-tertiary',
  Confirmed: 'text-fg-tertiary',
}

/** How fresh the live picture is: Live, Stale (60 s to 5 min), Unknown (nothing for 5 min). */
export function DataCell({ journey }: { journey: Pick<JourneyRow, 'data' | 'dataAgeSeconds'> }) {
  const age = journey.data === 'Stale' || journey.data === 'Unknown' ? ` · ${formatAge(journey.dataAgeSeconds)}` : ''
  return <span className={cn(DATA_STYLE[journey.data])}>{journey.data}{age}</span>
}

/** "5 (was 3)" when the platform changed. */
export function PlatformCell({ journey }: { journey: Pick<JourneyRow, 'platform' | 'originalPlatform' | 'platformState'> }) {
  if (!journey.platform) return <span className="text-fg-tertiary">–</span>
  const changed = journey.originalPlatform && journey.originalPlatform !== journey.platform
  if (!changed) return <span>{journey.platform}</span>
  return <StatusPill tone="changed">{journey.platform} (was {journey.originalPlatform})</StatusPill>
}
