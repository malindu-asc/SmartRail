import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react'
import { ChevronDown } from 'lucide-react'
import { cn } from '@/shared/utils'

const control =
  'w-full rounded-control border border-line-strong bg-surface px-3 text-fg placeholder:text-fg-tertiary focus-visible:outline-2 disabled:bg-soft disabled:text-fg-tertiary aria-[invalid=true]:border-cancelled'

interface FieldProps {
  label: string
  hint?: string
  error?: string | null
  /** The input; receives the generated id through the render prop so the label points at it. */
  children: (props: { id: string; 'aria-invalid': boolean | undefined; 'aria-describedby': string | undefined }) => ReactNode
}

/** A label, the control, a hint and an error beside it. Validation text goes here, not in a toast. */
export function Field({ label, hint, error, children }: FieldProps) {
  const id = useId()
  const noteId = `${id}-note`
  const note = error ?? hint
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-[13px] font-semibold text-fg">
        {label}
      </label>
      {children({ id, 'aria-invalid': error ? true : undefined, 'aria-describedby': note ? noteId : undefined })}
      {note && (
        <p id={noteId} className={cn('text-xs', error ? 'text-cancelled' : 'text-fg-tertiary')}>
          {note}
        </p>
      )}
    </div>
  )
}

export function TextInput({ className, ...rest }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn(control, 'h-10', className)} {...rest} />
}

export function TextArea({ className, ...rest }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={cn(control, 'min-h-20 py-2', className)} {...rest} />
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  /** Pill look used for filters above a table. */
  pill?: boolean
}

export function Select({ className, pill = false, children, ...rest }: SelectProps) {
  return (
    <span className="relative inline-flex w-full">
      <select
        className={cn(control, 'h-10 appearance-none pr-9', pill && 'rounded-full border-line font-semibold', className)}
        {...rest}
      >
        {children}
      </select>
      <ChevronDown aria-hidden className="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-fg-tertiary" />
    </span>
  )
}
