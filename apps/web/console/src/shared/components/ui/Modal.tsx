import { X } from 'lucide-react'
import { useEffect, useId, useRef, type ReactNode } from 'react'
import { cn } from '@/shared/utils'
import { Button } from './Button'

interface ModalProps {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  size?: 'sm' | 'md' | 'lg'
  /** Buttons, right-aligned. */
  footer?: ReactNode
  children: ReactNode
}

const SIZES = { sm: 'max-w-md', md: 'max-w-lg', lg: 'max-w-2xl' } as const

/**
 * A dialog for a decision or a short form. Built on the native <dialog>, so focus is trapped, Escape closes it and focus
 * returns to the button that opened it. Never nest modals.
 */
export function Modal({ open, onClose, title, description, size = 'md', footer, children }: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null)
  const titleId = useId()
  const descriptionId = useId()

  // Keep the native dialog in step with the `open` prop.
  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onCancel={(event) => {
        event.preventDefault()
        onClose()
      }}
      onClick={(event) => {
        if (event.target === ref.current) onClose()
      }}
      className={cn('m-auto w-[calc(100%-2rem)] rounded-panel border border-line bg-surface p-0 text-fg shadow-modal', SIZES[size])}
    >
      {open && (
        <div className="flex max-h-[85vh] flex-col">
          <div className="flex items-start justify-between gap-4 px-6 pt-5">
            <div>
              <h2 id={titleId} className="text-lg font-bold">
                {title}
              </h2>
              {description && (
                <p id={descriptionId} className="mt-1 text-fg-secondary">
                  {description}
                </p>
              )}
            </div>
            <button type="button" onClick={onClose} aria-label="Close" className="-mr-2 rounded-control p-2 text-fg-tertiary hover:bg-soft hover:text-fg">
              <X aria-hidden className="size-5" />
            </button>
          </div>
          <div className="overflow-y-auto px-6 py-4">{children}</div>
          {footer && <div className="flex justify-end gap-3 border-t border-line px-6 py-4">{footer}</div>}
        </div>
      )}
    </dialog>
  )
}

interface ConfirmModalProps {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  /** The verb on the main button: "Cancel journey", not "OK". */
  confirmLabel: string
  tone?: 'primary' | 'danger'
  pending?: boolean
  onConfirm: () => void
  children?: ReactNode
  /** Disables the main button until the form inside is valid. */
  confirmDisabled?: boolean
}

/** A yes/no decision, optionally with fields (a reason) as children. */
export function ConfirmModal({ open, onClose, title, description, confirmLabel, tone = 'primary', pending = false, onConfirm, children, confirmDisabled }: ConfirmModalProps) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      description={description}
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={pending}>
            Keep as it is
          </Button>
          <Button variant={tone === 'danger' ? 'danger' : 'primary'} onClick={onConfirm} loading={pending} disabled={confirmDisabled}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      {children}
    </Modal>
  )
}
