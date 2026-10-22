import { createContext } from 'react'

/** What GET /api/me returns. */
export interface SessionUser {
  subject: string | null
  userName: string | null
  name: string | null
  roles: string[]
  permissions: string[]
  consoles: string[]
}

export interface AuthContextValue {
  user: SessionUser
  permissions: ReadonlySet<string>
  /** True when the person holds the permission. The API enforces it again on every call. */
  can: (permission: string) => boolean
  /** True when the person holds at least one of them. */
  canAny: (permissions: readonly string[]) => boolean
  signOut: () => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
