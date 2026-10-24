import { describe, expect, it } from 'vitest'
import { sessionFor } from '@/test/utils'
import { isAdminConsole, NAV_ITEMS, sidebarItems } from './navigation'

const labels = (role: Parameters<typeof sessionFor>[0]) => sidebarItems(sessionFor(role).permissions).map((item) => item.to)

describe('navigation by role', () => {
  it('gives the controller the operations screens but no timetable editing and no administration', () => {
    const paths = labels('controller')
    expect(paths).toEqual(expect.arrayContaining(['/dashboard', '/live-board', '/disruptions', '/driver-messages', '/incidents']))
    expect(paths).not.toContain('/reports')
    expect(paths).not.toContain('/audit')
    expect(paths.some((path) => path.startsWith('/admin'))).toBe(false)
  })

  it('gives the scheduler planning, timetables and planned works but not incidents or driver messages', () => {
    const paths = labels('scheduler')
    expect(paths).toEqual(expect.arrayContaining(['/planning', '/timetables', '/disruptions', '/live-board']))
    expect(paths).not.toContain('/incidents')
    expect(paths).not.toContain('/driver-messages')
    expect(paths).not.toContain('/audit')
  })

  it('gives the administrator only the admin screens, the access audit log and no operations', () => {
    const paths = labels('admin')
    expect(paths).toEqual(['/admin', '/admin/users', '/admin/roles', '/admin/devices', '/admin/audit'])
    expect(isAdminConsole(sessionFor('admin').permissions)).toBe(true)
  })

  it('gives a station master the board and routes only (their own stations are enforced by the API)', () => {
    expect(labels('station_master')).toEqual(['/live-board', '/routes'])
  })

  it('gives a driver or a passenger nothing', () => {
    expect(labels('driver')).toEqual([])
    expect(labels('passenger')).toEqual([])
  })

  it('does not mark the controller console as the admin console', () => {
    expect(isAdminConsole(sessionFor('controller').permissions)).toBe(false)
  })

  it('has a permission on every item, so no screen is open to everyone by accident', () => {
    for (const item of NAV_ITEMS) expect(item.permission.length).toBeGreaterThan(0)
  })
})
