import { Compass, LockKeyhole } from 'lucide-react'
import { Link, Navigate } from 'react-router-dom'
import { homePath, useAuth } from '@/shared/auth'

/** "/": send each person to the home of their role. */
export function HomeRedirect() {
  const { user, permissions } = useAuth()
  return <Navigate to={homePath(user.roles, permissions)} replace />
}

/** Signed in, but the role has no console (a passenger or a driver opened the staff console). */
export function NoAccessPage() {
  const { signOut } = useAuth()
  return (
    <div className="flex min-h-screen items-center justify-center bg-page p-6">
      <div className="w-full max-w-md rounded-panel border border-line bg-surface p-8 text-center shadow-card">
        <span className="mx-auto mb-4 flex size-12 items-center justify-center rounded-full bg-soft text-fg-secondary"><LockKeyhole aria-hidden className="size-6" /></span>
        <h1 className="text-xl font-bold">This console is for staff</h1>
        <p className="mt-2 text-fg-secondary">Your account does not have a role that uses the operations console. If you should have access, ask a system administrator.</p>
        <button type="button" onClick={signOut} className="mt-6 h-10 rounded-control bg-primary px-4 font-semibold text-on-primary hover:bg-primary-pressed">Sign out</button>
      </div>
    </div>
  )
}

export function NotFoundPage() {
  const { user, permissions } = useAuth()
  return (
    <div className="mx-auto flex max-w-md flex-col items-center px-6 py-24 text-center">
      <span className="mb-4 flex size-12 items-center justify-center rounded-full bg-soft text-fg-secondary"><Compass aria-hidden className="size-6" /></span>
      <h1 className="text-xl font-bold">We could not find that page</h1>
      <p className="mt-2 text-fg-secondary">The address may be wrong, or the page may have moved.</p>
      <Link to={homePath(user.roles, permissions)} className="mt-6 inline-flex h-10 items-center rounded-control bg-primary px-4 font-semibold text-on-primary hover:bg-primary-pressed">Go to your home</Link>
    </div>
  )
}
