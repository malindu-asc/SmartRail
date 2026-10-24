import { ShieldAlert } from 'lucide-react'
import { Link } from 'react-router-dom'
import { homePath } from './permissions'
import { useAuth } from './useAuth'

/** 403: the person is signed in but their role does not include this screen. */
export function ForbiddenPage() {
  const { user, permissions } = useAuth()
  return (
    <div className="mx-auto flex max-w-md flex-col items-center px-6 py-24 text-center">
      <span className="mb-4 flex size-12 items-center justify-center rounded-full bg-cancelled-bg text-cancelled">
        <ShieldAlert aria-hidden className="size-6" />
      </span>
      <h1 className="text-xl font-bold text-fg">You do not have access to this</h1>
      <p className="mt-2 text-fg-secondary">Your role does not include this screen. If you need it, ask a system administrator.</p>
      <Link
        to={homePath(user.roles, permissions)}
        className="mt-6 inline-flex h-10 items-center rounded-control bg-primary px-4 font-semibold text-on-primary hover:bg-primary-pressed"
      >
        Go to your home
      </Link>
    </div>
  )
}
