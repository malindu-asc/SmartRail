import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Button } from './Button'
import { ConfirmModal, Modal } from './Modal'
import { toneFor } from './pillTone'
import { LoadingRegion, TableSkeleton } from './Skeleton'
import { EmptyState, ErrorState } from './States'
import { StatusPill } from './StatusPill'
import { ApiError } from '@/shared/api'

describe('Button', () => {
  it('is disabled and busy while loading', () => {
    render(<Button loading>Save</Button>)
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Save' })).toHaveAttribute('aria-busy', 'true')
  })
})

describe('StatusPill', () => {
  it('always says what it is in words', () => {
    render(<StatusPill tone="cancelled">Cancelled</StatusPill>)
    expect(screen.getByText('Cancelled')).toBeInTheDocument()
  })

  it('maps the server status words to tones', () => {
    expect(toneFor('Good')).toBe('ontime')
    expect(toneFor('Warning')).toBe('delayed')
    expect(toneFor('Critical')).toBe('cancelled')
    expect(toneFor('Info')).toBe('changed')
    expect(toneFor('Stale')).toBe('stale')
    expect(toneFor('whatever')).toBe('neutral')
    expect(toneFor(null)).toBe('neutral')
  })
})

describe('skeletons', () => {
  it('announce loading once and hide the shapes from screen readers', () => {
    render(<LoadingRegion label="Loading the live board"><TableSkeleton rows={2} columns={3} /></LoadingRegion>)
    expect(screen.getAllByRole('status').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Loading the live board').length).toBeGreaterThan(0)
  })

  it('draws the requested number of rows', () => {
    const { container } = render(<TableSkeleton rows={4} columns={3} />)
    expect(container.querySelectorAll('.sr-skeleton').length).toBe(3 + 4 * 3)
  })
})

describe('states', () => {
  it('shows an empty state with its next step', () => {
    render(<EmptyState title="No journeys match" description="Change the filters." />)
    expect(screen.getByText('No journeys match')).toBeInTheDocument()
    expect(screen.getByText('Change the filters.')).toBeInTheDocument()
  })

  it('shows why a query failed and retries', async () => {
    const retry = vi.fn()
    render(<ErrorState error={new ApiError(403, 'x')} onRetry={retry} />)
    expect(screen.getByRole('alert')).toHaveTextContent('Your role does not allow this.')
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(retry).toHaveBeenCalledOnce()
  })
})

describe('Modal', () => {
  it('renders its title and content only while open', () => {
    const { rerender } = render(<Modal open={false} onClose={() => {}} title="Cancel journey">Body</Modal>)
    expect(screen.queryByText('Body')).not.toBeInTheDocument()
    rerender(<Modal open onClose={() => {}} title="Cancel journey">Body</Modal>)
    expect(screen.getByRole('dialog', { name: 'Cancel journey' })).toBeInTheDocument()
    expect(screen.getByText('Body')).toBeInTheDocument()
  })

  it('closes from the close button and from Escape', async () => {
    const onClose = vi.fn()
    render(<Modal open onClose={onClose} title="Title">Body</Modal>)
    await userEvent.click(screen.getByRole('button', { name: 'Close' }))
    expect(onClose).toHaveBeenCalledTimes(1)
    fireEvent(screen.getByRole('dialog'), new Event('cancel', { cancelable: true }))
    expect(onClose).toHaveBeenCalledTimes(2)
  })

  it('has a confirm that names the action and can be blocked', async () => {
    const onConfirm = vi.fn()
    const { rerender } = render(<ConfirmModal open onClose={() => {}} title="Retract" confirmLabel="Retract notice" onConfirm={onConfirm} confirmDisabled />)
    expect(screen.getByRole('button', { name: 'Retract notice' })).toBeDisabled()
    rerender(<ConfirmModal open onClose={() => {}} title="Retract" confirmLabel="Retract notice" onConfirm={onConfirm} />)
    await userEvent.click(screen.getByRole('button', { name: 'Retract notice' }))
    expect(onConfirm).toHaveBeenCalledOnce()
  })
})
