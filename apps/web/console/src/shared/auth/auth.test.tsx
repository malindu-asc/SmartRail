import { screen } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { renderApp, sessionFor } from '@/test/utils'
import { Can } from './Can'
import { homePath, hasAny, Permissions, primaryRoleLabel } from './permissions'
import { RequirePermission } from './RequirePermission'

describe('permissions', () => {
  it('sends an administrator to the admin console and a controller to the dashboard', () => {
    const admin = sessionFor('admin')
    const controller = sessionFor('controller')
    expect(homePath(admin.user.roles, admin.permissions)).toBe('/admin')
    expect(homePath(controller.user.roles, controller.permissions)).toBe('/dashboard')
  })

  it('sends a station master to the live board and a passenger to the no-access page', () => {
    const master = sessionFor('station_master')
    const passenger = sessionFor('passenger')
    expect(homePath(master.user.roles, master.permissions)).toBe('/live-board')
    expect(homePath(passenger.user.roles, passenger.permissions)).toBe('/no-access')
  })

  it('hasAny is true for an empty requirement and when one of the permissions is held', () => {
    const held = new Set<string>([Permissions.OpsRead])
    expect(hasAny(held, [])).toBe(true)
    expect(hasAny(held, [Permissions.JourneyCancel, Permissions.OpsRead])).toBe(true)
    expect(hasAny(held, [Permissions.JourneyCancel])).toBe(false)
  })

  it('names the most senior role', () => {
    expect(primaryRoleLabel(['driver', 'controller'])).toBe('Network controller')
    expect(primaryRoleLabel([])).toBe('Staff')
  })
})

describe('<Can>', () => {
  it('shows its children only to a role that holds the permission', () => {
    renderApp(<Can permission={Permissions.JourneyCancel}><button>Cancel journey</button></Can>, { role: 'controller' })
    expect(screen.getByRole('button', { name: 'Cancel journey' })).toBeInTheDocument()
  })

  it('hides them from a role that does not, and shows the fallback', () => {
    renderApp(<Can permission={Permissions.JourneyCancel} fallback={<span>No access</span>}><button>Cancel journey</button></Can>, { role: 'scheduler' })
    expect(screen.queryByRole('button', { name: 'Cancel journey' })).not.toBeInTheDocument()
    expect(screen.getByText('No access')).toBeInTheDocument()
  })

  it('accepts a list and needs only one', () => {
    renderApp(<Can permission={[Permissions.JourneyCancel, Permissions.TimetableDraft]}><span>Either</span></Can>, { role: 'scheduler' })
    expect(screen.getByText('Either')).toBeInTheDocument()
  })
})

describe('<RequirePermission>', () => {
  function guarded(role: Parameters<typeof renderApp>[1] extends infer O ? NonNullable<O>['role'] : never) {
    renderApp(
      <Routes>
        <Route element={<RequirePermission permission={Permissions.UserManage} />}>
          <Route path="/admin" element={<h1>Users screen</h1>} />
        </Route>
      </Routes>,
      { role, route: '/admin' },
    )
  }

  it('opens the screen for the administrator', () => {
    guarded('admin')
    expect(screen.getByRole('heading', { name: 'Users screen' })).toBeInTheDocument()
  })

  it.each(['controller', 'scheduler', 'station_master', 'driver'] as const)('shows the 403 page to a %s', (role) => {
    guarded(role)
    expect(screen.queryByRole('heading', { name: 'Users screen' })).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /do not have access/i })).toBeInTheDocument()
  })
})
