import type { HTMLAttributes, ReactNode, TdHTMLAttributes, ThHTMLAttributes } from 'react'
import { cn } from '@/shared/utils'
import { Card } from './Card'

/** A bordered, scrollable table. Rows and cells below carry the spacing so pages only supply content. */
export function Table({ children, caption }: { children: ReactNode; caption: string }) {
  return (
    <Card className="overflow-x-auto">
      <table className="w-full border-collapse text-left">
        <caption className="sr-only">{caption}</caption>
        {children}
      </table>
    </Card>
  )
}

export function THead({ children }: { children: ReactNode }) {
  return (
    <thead>
      <tr className="border-b border-line bg-soft">{children}</tr>
    </thead>
  )
}

export function Th({ className, ...rest }: ThHTMLAttributes<HTMLTableCellElement>) {
  return <th scope="col" className={cn('whitespace-nowrap px-4 py-3 text-[13px] font-medium text-fg-secondary', className)} {...rest} />
}

interface TrProps extends HTMLAttributes<HTMLTableRowElement> {
  /** Shades the row: it needs attention. */
  tone?: 'attention' | 'danger' | 'good'
}

export function Tr({ className, tone, ...rest }: TrProps) {
  return (
    <tr
      className={cn(
        'border-b border-line last:border-0',
        tone === 'attention' && 'bg-delayed-bg/50',
        tone === 'danger' && 'bg-cancelled-bg/50',
        tone === 'good' && 'bg-primary-subtle/50',
        rest.onClick && 'cursor-pointer hover:bg-soft',
        className,
      )}
      {...rest}
    />
  )
}

export function Td({ className, ...rest }: TdHTMLAttributes<HTMLTableCellElement>) {
  return <td className={cn('px-4 py-3.5 align-middle', className)} {...rest} />
}
