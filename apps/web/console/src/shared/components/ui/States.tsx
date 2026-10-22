import { AlertCircle, Inbox } from 'lucide-react'
import type { ReactNode } from 'react'
import { errorMessage } from '@/shared/api'
import { Button } from './Button'

/** Nothing to show yet: say why and what to do next. */
export function EmptyState({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center rounded-card border border-dashed border-line-strong bg-surface px-6 py-14 text-center">
      <span className="mb-3 flex size-11 items-center justify-center rounded-full bg-soft text-fg-tertiary">
        <Inbox aria-hidden className="size-5" />
      </span>
      <p className="font-semibold text-fg">{title}</p>
      {description && <p className="mt-1 max-w-sm text-fg-secondary">{description}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  )
}

/** A query failed: show the reason in words and let the person retry. */
export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div role="alert" className="flex flex-col items-center rounded-card border border-line bg-surface px-6 py-14 text-center">
      <span className="mb-3 flex size-11 items-center justify-center rounded-full bg-cancelled-bg text-cancelled">
        <AlertCircle aria-hidden className="size-5" />
      </span>
      <p className="font-semibold text-fg">We could not load this</p>
      <p className="mt-1 max-w-sm text-fg-secondary">{errorMessage(error)}</p>
      {onRetry && (
        <Button variant="secondary" className="mt-4" onClick={onRetry}>
          Try again
        </Button>
      )}
    </div>
  )
}
