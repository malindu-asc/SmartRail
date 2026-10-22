import type { HTMLAttributes, ReactNode } from 'react'
import { cn } from '@/shared/utils'

export function Card({ className, ...rest }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('rounded-card border border-line bg-surface shadow-card', className)} {...rest} />
}

interface CardHeaderProps {
  title: string
  /** A small badge or note after the title. */
  meta?: ReactNode
  /** A button or link on the right. */
  action?: ReactNode
}

export function CardHeader({ title, meta, action }: CardHeaderProps) {
  return (
    <div className="flex items-center justify-between gap-3 border-b border-line px-5 py-4">
      <div className="flex items-center gap-3">
        <h2 className="text-base font-semibold text-fg">{title}</h2>
        {meta}
      </div>
      {action}
    </div>
  )
}
