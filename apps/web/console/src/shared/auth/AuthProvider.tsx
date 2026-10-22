import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, configureHttpClient } from '@/shared/api'
import { AuthContext, type AuthContextValue, type SessionUser } from './context'
import { currentToken, signOutOfKeycloak, startSignIn } from './keycloak'
import { hasAny } from './permissions'
import { AuthScreen } from './AuthScreen'

type State = { status: 'loading' } | { status: 'error'; message: string } | { status: 'ready'; user: SessionUser }

/**
 * Signs the person in (Keycloak), loads who they are and what they may do (GET /api/me), and only then renders the console.
 * Features read it through useAuth / useCan and never touch tokens.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false

    async function bootstrap() {
      try {
        configureHttpClient({ getToken: currentToken, onUnauthorized: signOutOfKeycloak })
        await startSignIn()
        const user = await api.get<SessionUser>('/me')
        if (!cancelled) setState({ status: 'ready', user })
      } catch (error) {
        if (!cancelled) setState({ status: 'error', message: error instanceof Error ? error.message : 'Sign-in failed.' })
      }
    }

    void bootstrap()
    return () => {
      cancelled = true
    }
  }, [])

  const value = useMemo<AuthContextValue | null>(() => {
    if (state.status !== 'ready') return null
    const permissions = new Set(state.user.permissions)
    return {
      user: state.user,
      permissions,
      can: (permission) => permissions.has(permission),
      canAny: (required) => hasAny(permissions, required),
      signOut: signOutOfKeycloak,
    }
  }, [state])

  if (!value) return <AuthScreen failed={state.status === 'error' ? state.message : null} />
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
