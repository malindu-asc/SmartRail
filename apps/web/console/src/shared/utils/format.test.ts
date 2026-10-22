import { describe, expect, it } from 'vitest'
import { formatAge, formatAgo, formatDelay, formatTime, humanize, initials, localInputToUtc, parseClock, todayIso, toLocalInput } from './format'

describe('formatting', () => {
  it('shows times in the network time zone (UTC+5:30)', () => {
    expect(formatTime('2026-10-14T03:00:00Z')).toBe('08:30')
    expect(formatTime(null)).toBe('–')
    expect(formatTime('not a date')).toBe('–')
  })

  it('works out the service date in the network time zone', () => {
    expect(todayIso(new Date('2026-10-13T20:00:00Z'))).toBe('2026-10-14')
  })

  it('writes delays with a sign and a dash for unknown', () => {
    expect(formatDelay(7)).toBe('+7')
    expect(formatDelay(0)).toBe('0')
    expect(formatDelay(null)).toBe('–')
  })

  it('writes ages in seconds, then minutes', () => {
    expect(formatAge(45)).toBe('45 s')
    expect(formatAge(200)).toBe('3 min')
  })

  it('writes how long ago something happened', () => {
    const now = new Date('2026-10-14T10:00:00Z')
    expect(formatAgo('2026-10-14T09:59:40Z', now)).toBe('just now')
    expect(formatAgo('2026-10-14T09:55:00Z', now)).toBe('5 min ago')
    expect(formatAgo('2026-10-14T07:00:00Z', now)).toBe('3 h ago')
  })

  it('takes initials and humanizes codes', () => {
    expect(initials('Sarah Perera')).toBe('SP')
    expect(initials('Madonna')).toBe('M')
    expect(initials(null)).toBe('?')
    expect(humanize('signal_fault')).toBe('Signal fault')
    expect(humanize('AwaitingApproval')).toBe('Awaiting approval')
  })

  it('parses clock times, hours may pass 24', () => {
    expect(parseClock('07:45')).toBe(465)
    expect(parseClock('24:25')).toBe(1465)
    expect(parseClock('7:5')).toBeNull()
    expect(parseClock('')).toBeNull()
  })

  it('round-trips a local input value to UTC and back', () => {
    const value = '2026-10-14T08:30'
    expect(toLocalInput(new Date(localInputToUtc(value)))).toBe(value)
  })
})
