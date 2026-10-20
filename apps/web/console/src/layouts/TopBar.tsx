import { useIsFetching, useQueryClient } from '@tanstack/react-query'
import { Lock, Moon, RefreshCw, Sun } from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { isAdminConsole, NAV_ITEMS } from '@/app/navigation'
import { useAuth } from '@/shared/auth'
import { useTheme } from '@/shared/hooks'
import { cn, formatTime } from '@/shared/utils'

/** Title of the current screen, the "cannot operate trains" reminder for administrators, refresh and theme. */
export function TopBar() {
  const { pathname } = useLocation()
  const { permissions } = useAuth()
  const { theme, toggle } = useTheme()
  const queryClient = useQueryClient()
  const fetching = useIsFetching() > 0
  const admin = isAdminConsole(permissions)

  const title = [...NAV_ITEMS].sort((a, b) => b.to.length - a.to.length).find((item) => pathname === item.to || pathname.startsWith(`${item.to}/`))?.label ?? 'SmartRail'
  const lastSync = queryClient
    .getQueryCache()
    .getAll()
    .reduce((latest, query) => Math.max(latest, query.state.dataUpdatedAt), 0)

  return (
    <header className="sticky top-0 z-10 flex h-16 items-center justify-between gap-4 border-b border-line bg-surface/95 px-8 backdrop-blur">
      <p className="text-lg font-bold text-fg">{title}</p>
      <div className="flex items-center gap-3">
        {admin ? (
          <span className="inline-flex items-center gap-2 rounded-full border border-line bg-soft px-3 py-1.5 text-xs font-medium text-fg-secondary">
            <Lock aria-hidden className="size-3.5" />
            Cannot operate trains
          </span>
        ) : (
          <span className="inline-flex items-center gap-2 rounded-full bg-on-time-bg px-3 py-1.5 text-xs font-medium text-on-time">
            <RefreshCw aria-hidden className={cn('size-3.5', fetching && 'animate-spin')} />
            Live{lastSync > 0 && ` · updated ${formatTime(new Date(lastSync))}`}
          </span>
        )}
        <button
          type="button"
          onClick={toggle}
          aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
          className="rounded-full border border-line p-2 text-fg-secondary hover:bg-soft hover:text-fg"
        >
          {theme === 'dark' ? <Sun aria-hidden className="size-4" /> : <Moon aria-hidden className="size-4" />}
        </button>
      </div>
    </header>
  )
}
