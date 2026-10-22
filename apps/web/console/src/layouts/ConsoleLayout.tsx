import { Suspense, useState } from 'react'
import { Outlet } from 'react-router-dom'
import { SignOutModal } from '@/features/auth/SignOutModal'
import { Skeleton } from '@/shared/components/ui'
import { Sidebar } from './Sidebar'
import { TopBar } from './TopBar'

/** The shared shell: navigation on the left, the title bar on top, the screen below. */
export function ConsoleLayout() {
  const [signingOut, setSigningOut] = useState(false)

  return (
    <div className="flex min-h-screen bg-page">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50 focus:rounded-control focus:bg-surface focus:px-3 focus:py-2">
        Skip to content
      </a>
      <Sidebar onSignOut={() => setSigningOut(true)} />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar />
        <main id="main" className="flex-1 px-8 py-6">
          <Suspense fallback={<Skeleton className="h-64 w-full" />}>
            <Outlet />
          </Suspense>
        </main>
      </div>
      <SignOutModal open={signingOut} onClose={() => setSigningOut(false)} />
    </div>
  )
}
