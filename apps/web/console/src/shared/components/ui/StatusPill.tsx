import { cva, type VariantProps } from 'class-variance-authority'
import type { ReactNode } from 'react'
import { cn } from '@/shared/utils'

const pillVariants = cva('inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-medium', {
  variants: {
    tone: {
      ontime: 'bg-on-time-bg text-on-time',
      delayed: 'bg-delayed-bg text-delayed',
      cancelled: 'bg-cancelled-bg text-cancelled',
      stale: 'bg-stale-bg text-stale',
      changed: 'bg-changed-bg text-changed',
      brand: 'bg-primary-subtle text-primary',
      neutral: 'bg-soft text-fg-secondary',
    },
  },
  defaultVariants: { tone: 'neutral' },
})

export type PillTone = NonNullable<VariantProps<typeof pillVariants>['tone']>

/** A coloured dot and a word. Colour is never the only signal: the text always says what it is. */
export function StatusPill({ tone, children, className }: { tone?: PillTone; children: ReactNode; className?: string }) {
  return (
    <span className={cn(pillVariants({ tone }), className)}>
      <span aria-hidden className="size-1.5 rounded-full bg-current" />
      {children}
    </span>
  )
}
