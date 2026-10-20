import { Link } from 'react-router-dom'
import { Card, CardHeader, StatCard, StatusPill } from '@/shared/components/ui'
import { roleLabel } from '@/shared/auth'
import { formatAgo } from '@/shared/utils'
import type { AdminDashboard } from '../types'

const ATTENTION_TONE: Record<string, 'cancelled' | 'delayed'> = { AccountLocked: 'cancelled', DeviceOffline: 'cancelled', LeaverStillActive: 'delayed', DeviceLate: 'delayed' }

function attentionLink(item: AdminDashboard['needsAttention'][number]): string {
  return item.targetType === 'device' ? '/admin/devices' : '/admin/users'
}

export function AdminDashboardView({ data }: { data: AdminDashboard }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard label="Staff accounts" value={data.staffAccounts} note={`${data.rolesInUse} roles in use`} />
        <StatCard label="Signed in now" value={data.signedInNow} />
        <StatCard label="Failed sign-ins" value={data.failedSignIns24h} tone={data.failedSignIns24h > 0 ? 'delayed' : undefined} note={`${data.lockedAccounts} locked · last 24 h`} />
        <StatCard label="Devices online" value={`${data.devices.online}/${data.devices.total}`} tone={data.devices.needAttention > 0 ? 'delayed' : 'ontime'} note={`${data.devices.needAttention} need attention`} />
      </div>

      <div className="grid gap-4 xl:grid-cols-[1.4fr_1fr]">
        <Card>
          <CardHeader title="Needs attention" meta={data.needsAttention.length > 0 ? <StatusPill tone="delayed">{data.needsAttention.length}</StatusPill> : undefined} />
          {data.needsAttention.length === 0 ? (
            <p className="px-5 py-8 text-center text-fg-secondary">Nothing needs attention right now.</p>
          ) : (
            <ul className="divide-y divide-line">
              {data.needsAttention.map((item) => (
                <li key={`${item.kind}-${item.targetId}`} className="flex items-center justify-between gap-4 px-5 py-3.5">
                  <div>
                    <p className="flex items-center gap-2 font-semibold">{item.title} <StatusPill tone={ATTENTION_TONE[item.kind] ?? 'neutral'}>{item.kind.replace(/([A-Z])/g, ' $1').trim()}</StatusPill></p>
                    <p className="text-fg-secondary">{item.detail}</p>
                  </div>
                  <Link to={attentionLink(item)} className="inline-flex h-8 items-center rounded-control bg-primary px-3 text-[13px] font-semibold text-on-primary hover:bg-primary-pressed">{item.action}</Link>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <div className="space-y-4">
          <Card>
            <CardHeader title="Staff by role" />
            <ul className="divide-y divide-line">
              {Object.entries(data.staffByRole).map(([role, count]) => (
                <li key={role} className="flex justify-between px-5 py-2.5"><span>{roleLabel(role)}</span><strong>{count}</strong></li>
              ))}
            </ul>
          </Card>
          <Card>
            <CardHeader title="Access review" />
            <p className="px-5 py-4 text-fg-secondary">
              {data.accessReview.daysLeft === null ? 'No review recorded yet.' : data.accessReview.daysLeft <= 0 ? <strong className="text-cancelled">A review is due now.</strong> : <>Next review in <strong className="text-fg">{data.accessReview.daysLeft} days</strong>.</>}
              {' '}<Link to="/admin/security" className="font-semibold text-primary hover:underline">Open security</Link>
            </p>
          </Card>
        </div>
      </div>

      <Card>
        <CardHeader title="Recent access changes" action={<Link to="/admin/audit" className="font-semibold text-fg-secondary hover:text-fg">Audit log</Link>} />
        {data.recentAccessChanges.length === 0 ? (
          <p className="px-5 py-6 text-fg-secondary">No access changes yet.</p>
        ) : (
          <ul className="divide-y divide-line">
            {data.recentAccessChanges.map((change) => (
              <li key={`${change.occurredAt}-${change.text}`} className="flex justify-between gap-4 px-5 py-3">
                <span>{change.text}</span>
                <span className="text-fg-tertiary">{change.actorName ?? 'System'} · {formatAgo(change.occurredAt)}</span>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  )
}
