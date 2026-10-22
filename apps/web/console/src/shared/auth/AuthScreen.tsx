import { Logo } from '@/shared/components/ui'

/** Shown while signing in, or when sign-in failed. */
export function AuthScreen({ failed }: { failed: string | null }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-page p-6">
      <div role="status" className="w-full max-w-sm rounded-panel border border-line bg-surface p-8 text-center shadow-card">
        <Logo variant="full" className="mx-auto" />
        <h1 className="mt-2 text-lg font-bold text-fg">Ops console</h1>
        {failed ? (
          <>
            <p className="mt-2 text-fg-secondary">We could not sign you in. {failed}</p>
            <button
              type="button"
              onClick={() => window.location.reload()}
              className="mt-5 h-10 rounded-control bg-primary px-4 font-semibold text-on-primary hover:bg-primary-pressed"
            >
              Try again
            </button>
          </>
        ) : (
          <p className="mt-2 text-fg-secondary">Signing you in…</p>
        )}
      </div>
    </div>
  )
}
