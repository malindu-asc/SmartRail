/** Shown while signing in, or when sign-in failed. */
export function AuthScreen({ failed }: { failed: string | null }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-page p-6">
      <div role="status" className="w-full max-w-sm rounded-panel border border-line bg-surface p-8 text-center shadow-card">
        <div className="mx-auto mb-4 flex size-10 items-center justify-center rounded-card bg-primary text-on-primary font-bold">SR</div>
        <h1 className="text-lg font-bold text-fg">SmartRail Ops console</h1>
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
