import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { AuthContext, type AuthContextValue, type SessionUser } from '@/shared/auth'
import { RolePermissionsForTests } from './rolePermissions'

/** A signed-in person with the permissions of a role, so tests read like the permission matrix. */
export function sessionFor(role: keyof typeof RolePermissionsForTests, overrides: Partial<SessionUser> = {}): AuthContextValue {
  const permissions = [...RolePermissionsForTests[role]]
  const user: SessionUser = { subject: `${role}-1`, userName: role, name: `Test ${role}`, roles: [role], permissions, consoles: [], ...overrides }
  const set = new Set(user.permissions)
  return {
    user,
    permissions: set,
    can: (permission) => set.has(permission),
    canAny: (required) => required.length === 0 || required.some((permission) => set.has(permission)),
    signOut: () => {},
  }
}

interface Options {
  role?: keyof typeof RolePermissionsForTests
  session?: AuthContextValue
  route?: string
}

/** Renders with sign-in, data cache and routing in place. */
export function renderApp(ui: ReactElement, { role = 'controller', session, route = '/' }: Options = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthContext.Provider value={session ?? sessionFor(role)}>
        <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
      </AuthContext.Provider>
    </QueryClientProvider>,
  )
}

/** Answers fetch with canned JSON by path. Anything not listed is a 404 problem. */
export function mockApi(routes: Record<string, unknown>): void {
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = new URL(String(input), 'http://localhost')
    const key = Object.keys(routes).find((path) => url.pathname + url.search === path || url.pathname === path)
    if (key === undefined) {
      return new Response(JSON.stringify({ title: 'Not found', code: 'Test.NotFound' }), { status: 404, headers: { 'Content-Type': 'application/json' } })
    }
    const body = routes[key]
    return new Response(body === undefined ? null : JSON.stringify(body), { status: body === undefined ? 204 : 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch
}
