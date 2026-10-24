import { useContext } from 'react'
import { AuthContext, type AuthContextValue } from './context'

/** The signed-in person. Only available below AuthProvider, which renders nothing until sign-in is done. */
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within an AuthProvider')
  return context
}

/** True when the person holds the permission. */
export function useCan(permission: string): boolean {
  return useAuth().can(permission)
}

/** True when the person holds at least one of the permissions. */
export function useCanAny(permissions: readonly string[]): boolean {
  return useAuth().canAny(permissions)
}
