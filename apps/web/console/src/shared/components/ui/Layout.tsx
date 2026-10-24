import type { ReactNode } from 'react'
import { cn } from '@/shared/utils'
import { Card } from './Card'

/** Page title row: heading, a line of context and the main actions on the right. */
export function PageHeader({ title, description, actions }: { title: string; description?: string; actions?: ReactNode }) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 className="text-2xl font-bold text-fg">{title}</h1>
        {description && <p className="mt-1 text-fg-secondary">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-3">{actions}</div>}
    </div>
  )
}

/** One headline number. */
export function StatCard({ label, value, note, tone }: { label: string; value: ReactNode; note?: string; tone?: 'ontime' | 'delayed' | 'cancelled' }) {
  return (
    <Card className="p-5">
      <p className="text-[13px] font-medium text-fg-secondary">{label}</p>
      <p className={cn('mt-1 text-[32px] font-bold leading-10', tone === 'ontime' && 'text-on-time', tone === 'delayed' && 'text-delayed', tone === 'cancelled' && 'text-cancelled')}>{value}</p>
      {note && <p className="mt-1 text-xs text-fg-secondary">{note}</p>}
    </Card>
  )
}

/** The row of filters above a table. */
export function FilterBar({ children, trailing }: { children: ReactNode; trailing?: ReactNode }) {
  return (
    <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
      <div className="flex flex-wrap items-center gap-3">{children}</div>
      {trailing && <div className="flex items-center gap-3">{trailing}</div>}
    </div>
  )
}

interface TabItem {
  value: string
  label: string
  count?: number
}

/** Tabs that live in the URL: the caller passes the current value and a setter. */
export function Tabs({ items, value, onChange, label }: { items: TabItem[]; value: string; onChange: (value: string) => void; label: string }) {
  return (
    <div role="tablist" aria-label={label} className="mb-4 inline-flex gap-1 rounded-full bg-soft p-1">
      {items.map((item) => (
        <button
          key={item.value}
          role="tab"
          type="button"
          aria-selected={item.value === value}
          onClick={() => onChange(item.value)}
          className={cn(
            'rounded-full px-4 py-1.5 text-[13px] font-semibold transition-colors',
            item.value === value ? 'bg-surface text-fg shadow-card' : 'text-fg-secondary hover:text-fg',
          )}
        >
          {item.label}
          {item.count !== undefined && <span className="ml-1.5 text-fg-tertiary">{item.count}</span>}
        </button>
      ))}
    </div>
  )
}
