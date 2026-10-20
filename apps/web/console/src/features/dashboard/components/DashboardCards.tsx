import { Hand, MessageSquare, ShieldCheck, UserX, WifiOff, AlertTriangle, PauseCircle, type LucideIcon } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Card, CardHeader, EmptyState, StatCard, StatusPill, toneFor } from '@/shared/components/ui'
import { cn, formatTime } from '@/shared/utils'
import type { Attention, Dashboard, DashboardDevices, DashboardDisruption, Kpis } from '../types'
import { attentionLink } from './attentionLink'

export function KpiRow({ kpis }: { kpis: Kpis }) {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-6">
      <StatCard label="Running" value={kpis.running} note="journeys on the network" />
      <StatCard label="On time" value={kpis.onTimePercent === null ? '–' : `${kpis.onTimePercent}%`} tone="ontime" note="within 5 min today" />
      <StatCard label="Delayed" value={kpis.delayed} tone="delayed" note={`${kpis.delayedOver15} over 15 min`} />
      <StatCard label="Cancelled" value={kpis.cancelled} tone="cancelled" note="today" />
      <StatCard label="No live data" value={kpis.noLiveData} note="for over 5 min" />
      <StatCard label="Disruptions" value={kpis.disruptionsActive} note={`${kpis.disruptionsAwaitingApproval} awaiting approval`} />
    </div>
  )
}

const ATTENTION_STYLE: Record<string, { icon: LucideIcon; box: string }> = {
  AssistanceRequested: { icon: Hand, box: 'bg-cancelled-bg text-cancelled' },
  NoLiveData: { icon: WifiOff, box: 'bg-stale-bg text-stale' },
  MessageNotAcknowledged: { icon: MessageSquare, box: 'bg-delayed-bg text-delayed' },
  NoDriver: { icon: UserX, box: 'bg-delayed-bg text-delayed' },
  NoUnit: { icon: UserX, box: 'bg-delayed-bg text-delayed' },
  UnitOutOfService: { icon: AlertTriangle, box: 'bg-delayed-bg text-delayed' },
  MajorDelay: { icon: AlertTriangle, box: 'bg-delayed-bg text-delayed' },
  Interrupted: { icon: PauseCircle, box: 'bg-delayed-bg text-delayed' },
  ApprovalNeeded: { icon: ShieldCheck, box: 'bg-changed-bg text-changed' },
  DisruptionExpired: { icon: AlertTriangle, box: 'bg-delayed-bg text-delayed' },
}

export function NeedsAttention({ items }: { items: Attention[] }) {
  return (
    <Card>
      <CardHeader title="Needs attention" meta={items.length > 0 ? <StatusPill tone="delayed">{items.length} {items.length === 1 ? 'item' : 'items'}</StatusPill> : undefined} />
      {items.length === 0 ? (
        <p className="px-5 py-8 text-center text-fg-secondary">Nothing needs attention right now.</p>
      ) : (
        <ul className="divide-y divide-line">
          {items.map((item) => {
            const style = ATTENTION_STYLE[item.kind] ?? { icon: AlertTriangle, box: 'bg-soft text-fg-secondary' }
            const Icon = style.icon
            return (
              <li key={`${item.kind}-${item.targetId}`} className="flex items-center gap-4 px-5 py-3.5">
                <span className={cn('flex size-10 shrink-0 items-center justify-center rounded-card', style.box)}>
                  <Icon aria-hidden className="size-5" />
                </span>
                <div className="min-w-0 flex-1">
                  <p className="font-semibold text-fg">{item.title}</p>
                  <p className="truncate text-fg-secondary">{item.detail}</p>
                </div>
                <Link
                  to={attentionLink(item)}
                  className={cn(
                    'inline-flex h-8 items-center rounded-control px-3 text-[13px] font-semibold',
                    item.action === 'Open' ? 'border border-line-strong bg-surface text-fg hover:bg-soft' : 'bg-primary text-on-primary hover:bg-primary-pressed',
                  )}
                >
                  {item.action}
                </Link>
              </li>
            )
          })}
        </ul>
      )}
    </Card>
  )
}

const DEVICE_LABEL: Record<string, string> = {
  PlatformDisplay: 'Platform displays',
  ConcourseBoard: 'Concourse boards',
  GateScanner: 'Gate scanners',
  Terminal: 'Terminals',
}

export function DevicesCard({ devices }: { devices: DashboardDevices }) {
  return (
    <Card>
      <CardHeader title="Station devices" />
      {devices.total === 0 ? (
        <p className="px-5 py-6 text-fg-secondary">No devices are registered yet.</p>
      ) : (
        <ul className="divide-y divide-line">
          {devices.byType.map((row) => {
            const percent = row.total === 0 ? 0 : Math.round((row.online / row.total) * 100)
            return (
              <li key={row.type} className="flex items-center justify-between gap-4 px-5 py-3.5">
                <div>
                  <p className="font-semibold">{DEVICE_LABEL[row.type] ?? row.type}</p>
                  <p className="text-xs text-fg-secondary">{row.online} of {row.total} online</p>
                </div>
                <div
                  role="progressbar"
                  aria-label={`${DEVICE_LABEL[row.type] ?? row.type} online`}
                  aria-valuenow={percent}
                  aria-valuemin={0}
                  aria-valuemax={100}
                  className="h-1.5 w-40 rounded-full bg-soft"
                >
                  <div className={cn('h-full rounded-full', percent >= 95 ? 'bg-on-time' : 'bg-delayed')} style={{ width: `${percent}%` }} />
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </Card>
  )
}

export function ActiveDisruptions({ items }: { items: DashboardDisruption[] }) {
  return (
    <Card>
      <CardHeader title="Active disruptions" action={<Link to="/disruptions" className="font-semibold text-fg-secondary hover:text-fg">View all</Link>} />
      {items.length === 0 ? (
        <div className="p-5"><EmptyState title="No active disruptions" /></div>
      ) : (
        <ul className="divide-y divide-line">
          {items.map((item) => (
            <li key={item.id}>
              <Link to={`/disruptions/${item.id}`} className="flex items-center gap-4 px-5 py-3.5 hover:bg-soft">
                <span className={cn('flex size-10 shrink-0 items-center justify-center rounded-card', toneFor(item.severity) === 'cancelled' ? 'bg-cancelled-bg text-cancelled' : 'bg-delayed-bg text-delayed')}>
                  <AlertTriangle aria-hidden className="size-5" />
                </span>
                <div className="min-w-0">
                  <p className="truncate font-semibold">{item.title}</p>
                  <p className="text-fg-secondary">{item.severity}{item.sinceUtc && ` · since ${formatTime(item.sinceUtc)}`} · {item.scope}</p>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}

/** One bar of the day: how many journeys are on time, delayed, cancelled or silent. */
export function JourneysToday({ kpis }: { kpis: Kpis }) {
  const onTime = Math.max(0, kpis.running - kpis.delayed - kpis.noLiveData)
  const parts = [
    { label: 'On time', value: onTime, bar: 'bg-on-time', dot: 'bg-on-time' },
    { label: 'Delayed', value: kpis.delayed, bar: 'bg-delayed', dot: 'bg-delayed' },
    { label: 'Cancelled', value: kpis.cancelled, bar: 'bg-cancelled', dot: 'bg-cancelled' },
    { label: 'No live data', value: kpis.noLiveData, bar: 'bg-line-strong', dot: 'bg-line-strong' },
  ]
  const total = parts.reduce((sum, part) => sum + part.value, 0)

  return (
    <Card>
      <CardHeader title="Journeys today" meta={<StatusPill>{kpis.running} running</StatusPill>} />
      <div className="px-5 py-5">
        <div className="flex h-2 overflow-hidden rounded-full bg-soft" role="img" aria-label={parts.map((p) => `${p.label} ${p.value}`).join(', ')}>
          {total > 0 && parts.filter((part) => part.value > 0).map((part) => <div key={part.label} className={part.bar} style={{ width: `${(part.value / total) * 100}%` }} />)}
        </div>
        <ul className="mt-3 flex flex-wrap gap-x-6 gap-y-1 text-fg-secondary">
          {parts.map((part) => (
            <li key={part.label} className="flex items-center gap-2">
              <span aria-hidden className={cn('size-2 rounded-full', part.dot)} />
              {part.label} <strong className="text-fg">{part.value}</strong>
            </li>
          ))}
        </ul>
      </div>
    </Card>
  )
}

export function DashboardGrid({ data }: { data: Dashboard }) {
  return (
    <div className="space-y-4">
      <KpiRow kpis={data.kpis} />
      <div className="grid gap-4 xl:grid-cols-[1.4fr_1fr]">
        <NeedsAttention items={data.needsAttention} />
        <div className="space-y-4">
          <DevicesCard devices={data.devices} />
          <ActiveDisruptions items={data.activeDisruptions} />
        </div>
      </div>
      <JourneysToday kpis={data.kpis} />
    </div>
  )
}
