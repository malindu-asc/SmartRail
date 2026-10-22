import { LogOut } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { isAdminConsole, sidebarItems, type NavItem } from '@/app/navigation'
import { primaryRoleLabel } from '@/shared/auth'
import { useAuth } from '@/shared/auth'
import { Logo } from '@/shared/components/ui'
import { cn, initials } from '@/shared/utils'

interface SidebarProps {
  onSignOut: () => void
}

/** The left navigation. It lists only what the signed-in person may open; the admin console gets the dark chrome. */
export function Sidebar({ onSignOut }: SidebarProps) {
  const { user, permissions } = useAuth()
  const admin = isAdminConsole(permissions)
  const items = sidebarItems(permissions)
  const name = user.name ?? user.userName ?? 'Signed in'

  return (
    <aside className={cn('sticky top-0 flex h-screen w-62 shrink-0 flex-col border-r', admin ? 'border-transparent bg-admin-nav text-admin-nav-fg' : 'border-line bg-surface text-fg')}>
      <div className="flex items-center gap-3 px-5 py-5">
        <Logo className="h-9" />
        <div className="leading-tight">
          <p className="font-bold">SmartRail</p>
          <p className={cn('text-xs', admin ? 'text-admin-nav-fg/70' : 'text-fg-secondary')}>{admin ? 'Admin console' : 'Ops console'}</p>
        </div>
      </div>

      <nav aria-label="Main" className="flex-1 space-y-1 overflow-y-auto px-3">
        {items.map((item) => (
          <SidebarLink key={item.to} item={item} admin={admin} />
        ))}
      </nav>

      <div className={cn('flex items-center gap-3 border-t px-4 py-4', admin ? 'border-white/10' : 'border-line')}>
        <span className={cn('flex size-9 shrink-0 items-center justify-center rounded-full text-xs font-bold', admin ? 'bg-admin-nav-active text-admin-nav-fg' : 'bg-primary-subtle text-primary')}>
          {initials(name)}
        </span>
        <div className="min-w-0 flex-1 leading-tight">
          <p className="truncate font-semibold">{name}</p>
          <p className={cn('truncate text-xs', admin ? 'text-admin-nav-fg/70' : 'text-fg-secondary')}>{primaryRoleLabel(user.roles)}</p>
        </div>
        <button
          type="button"
          onClick={onSignOut}
          aria-label="Sign out"
          className={cn('rounded-control p-2', admin ? 'hover:bg-admin-nav-active' : 'text-fg-secondary hover:bg-soft hover:text-fg')}
        >
          <LogOut aria-hidden className="size-5" />
        </button>
      </div>
    </aside>
  )
}

function SidebarLink({ item, admin }: { item: NavItem; admin: boolean }) {
  const Icon = item.icon
  return (
    <NavLink
      to={item.to}
      end={item.to === '/admin'}
      className={({ isActive }) =>
        cn(
          'flex h-10 items-center gap-3 rounded-control px-3 font-semibold transition-colors',
          isActive
            ? admin
              ? 'bg-admin-nav-active text-white'
              : 'bg-primary-subtle text-primary'
            : admin
              ? 'text-admin-nav-fg/80 hover:bg-admin-nav-active'
              : 'text-fg-secondary hover:bg-soft hover:text-fg',
        )
      }
    >
      <Icon aria-hidden className="size-5" />
      {item.label}
    </NavLink>
  )
}
