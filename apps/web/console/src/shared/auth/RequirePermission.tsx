import { Outlet } from 'react-router-dom'
import { ForbiddenPage } from './ForbiddenPage'
import { useCanAny } from './useAuth'

interface RequirePermissionProps {
  /** One permission, or a list where any one is enough. */
  permission: string | readonly string[]
}

/** Route guard: shows the child routes only to people who hold the permission, the 403 page to everyone else. */
export function RequirePermission({ permission }: RequirePermissionProps) {
  const required = typeof permission === 'string' ? [permission] : permission
  return useCanAny(required) ? <Outlet /> : <ForbiddenPage />
}
