import type { ReactNode } from 'react'
import { useCanAny } from './useAuth'

interface CanProps {
  /** One permission, or a list where any one is enough. */
  permission: string | readonly string[]
  /** Shown when the person lacks it. Most actions render nothing. */
  fallback?: ReactNode
  children: ReactNode
}

/** Renders its children only for people who hold the permission. The API still enforces it on every call. */
export function Can({ permission, fallback = null, children }: CanProps) {
  const required = typeof permission === 'string' ? [permission] : permission
  const allowed = useCanAny(required)
  return <>{allowed ? children : fallback}</>
}
