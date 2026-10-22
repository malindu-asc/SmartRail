import assert from 'node:assert/strict'
import { test } from 'node:test'
import { rowLabel, words } from './i18n.ts'
import { arrivalRow, boardSeconds, clock, dayOf, diffRows, pagesOf, departureRow, linkState, minutesUntil, noticesFor, platformModel } from './logic.ts'
import type { ArrivalItem, DepartureItem, Notice } from './types.ts'

const zone = 'Asia/Colombo'

function departure(overrides: Partial<DepartureItem> = {}): DepartureItem {
  return {
    journeyId: 'j1',
    trainNumber: '1001',
    routeCode: 'MAIN',
    destinationCode: 'RIV',
    destinationName: 'Riverside',
    scheduledDepartureUtc: '2026-10-21T01:00:00Z',
    expectedDepartureUtc: '2026-10-21T01:00:00Z',
    estimateSource: 'Scheduled',
    delayMinutes: null,
    platform: '1',
    platformState: 'Assigned',
    status: 'Planned',
    confidence: 'Scheduled',
    isStale: false,
    ageSeconds: null,
    atPlatform: false,
    ...overrides,
  }
}

function arrival(overrides: Partial<ArrivalItem> = {}): ArrivalItem {
  return {
    journeyId: 'a1',
    trainNumber: '1501',
    routeCode: 'MAIN',
    originCode: 'RIV',
    originName: 'Riverside',
    scheduledArrivalUtc: '2026-10-21T01:20:00Z',
    expectedArrivalUtc: '2026-10-21T01:20:00Z',
    estimateSource: 'Scheduled',
    delayMinutes: null,
    platform: '1',
    platformState: 'Assigned',
    status: 'Planned',
    confidence: 'Scheduled',
    isStale: false,
    ageSeconds: null,
    ...overrides,
  }
}

function notice(overrides: Partial<Notice> = {}): Notice {
  return {
    id: 'n1',
    title: 'Signal fault',
    message: 'Delays',
    severity: 'Minor',
    networkWide: false,
    stations: [{ code: 'CEN', name: 'Central' }],
    updatedAtUtc: '2026-10-21T01:00:00Z',
    expectedEndUtc: null,
    startsAtUtc: null,
    planned: false,
    ...overrides,
  }
}

const nowMs = Date.parse('2026-10-21T01:00:00Z')
const live = { link: 'live', timeZone: zone, nowMs } as const

test('the connection is live for a minute, then delayed, then offline after five (REQ-DSP-004)', () => {
  assert.equal(linkState(0), 'live')
  assert.equal(linkState(59_999), 'live')
  assert.equal(linkState(60_000), 'delayed')
  assert.equal(linkState(299_999), 'delayed')
  assert.equal(linkState(300_000), 'offline')
})

test('times are shown in the network time zone, not the screen\'s', () => {
  assert.equal(clock('2026-10-21T01:00:00Z', zone), '06:30')
  assert.equal(clock('2026-10-21T18:55:00Z', zone), '00:25')
})

test('a countdown rounds up so one minute shows until it is really over', () => {
  assert.equal(minutesUntil(60_000 * 7, 0), 7)
  assert.equal(minutesUntil(60_000 * 7, 1_000), 7)
  assert.equal(minutesUntil(60_000 * 7, 60_000 * 7), 0)
  assert.equal(minutesUntil(1_000, 0), 1)
})

test('a train with no delay is on time', () => {
  const row = departureRow(departure(), live)
  assert.deepEqual([row.status, row.label, row.time, row.name, row.platform], ['on_time', 'On time', '06:30', 'Riverside', '1'])
})

test('a late train says when it is expected, not how late', () => {
  const row = departureRow(departure({ delayMinutes: 12, estimateSource: 'Live', confidence: 'Live', expectedDepartureUtc: '2026-10-21T01:12:00Z' }), live)
  assert.deepEqual([row.status, row.label], ['delayed', 'Expected 06:42'])
})

test('a cancelled train is shown as cancelled, whatever the connection', () => {
  assert.equal(departureRow(departure({ status: 'Cancelled' }), live).status, 'cancelled')
  assert.equal(departureRow(departure({ status: 'Cancelled' }), { link: 'offline', timeZone: zone, nowMs }).status, 'cancelled')
})

test('a platform change is shown as a change', () => {
  const row = departureRow(departure({ platformState: 'Changed', platform: '2' }), live)
  assert.deepEqual([row.status, row.label, row.platform], ['changed', 'Platform changed', '2'])
})

test('when the display has lost its connection for five minutes every train is the timetable, never a live status', () => {
  const row = departureRow(departure({ delayMinutes: 12, estimateSource: 'Live', confidence: 'Live' }), { link: 'offline', timeZone: zone, nowMs })
  assert.deepEqual([row.status, row.label], ['scheduled', 'Scheduled'])
})

test('a train the system has not heard from is the timetable, not a guess', () => {
  const silent = departureRow(departure({ status: 'Running', confidence: 'Unknown', isStale: true }), live)
  assert.equal(silent.status, 'scheduled')
})

test('a train that has left says so', () => {
  const row = departureRow(departure({ status: 'Running', estimateSource: 'Confirmed', confidence: 'Confirmed' }), live)
  assert.deepEqual([row.status, row.label], ['on_time', 'Departed'])
})

test('an arrival names where the train started and says when it has arrived', () => {
  const row = arrivalRow(arrival({ status: 'Arrived', estimateSource: 'Confirmed', confidence: 'Confirmed' }), live)
  assert.deepEqual([row.name, row.label], ['Riverside', 'Arrived'])
})

test('a late arrival says when it is expected', () => {
  const row = arrivalRow(arrival({ delayMinutes: 5, estimateSource: 'Live', confidence: 'Live', expectedArrivalUtc: '2026-10-21T01:25:00Z' }), live)
  assert.deepEqual([row.status, row.label], ['delayed', 'Expected 06:55'])
})

test('notices: the station and the whole network, worst first, never works that start later', () => {
  const list = [
    notice({ id: 'minor', severity: 'Minor' }),
    notice({ id: 'severe-network', severity: 'Severe', networkWide: true, stations: [] }),
    notice({ id: 'other-station', stations: [{ code: 'MIL', name: 'Mill Lane' }] }),
    notice({ id: 'later', planned: true, severity: 'Severe' }),
  ]
  assert.deepEqual(noticesFor(list, 'CEN').map((n) => n.id), ['severe-network', 'minor'])
})

test('a platform display shows the next train and two more', () => {
  const list = [
    departure({ journeyId: '1', platform: '1' }),
    departure({ journeyId: '2', platform: '1' }),
    departure({ journeyId: '3', platform: '1' }),
    departure({ journeyId: '4', platform: '1' }),
  ]
  const model = platformModel(list, null)
  assert.equal(model.next?.journeyId, '1')
  assert.deepEqual(model.after.map((d) => d.journeyId), ['2', '3'])
})

test('a platform display only shows its own platform when it has one', () => {
  const list = [departure({ journeyId: '1', platform: '2' }), departure({ journeyId: '2', platform: '1' })]
  assert.equal(platformModel(list, '1').next?.journeyId, '2')
})

test('a train that has left is not the next train', () => {
  const list = [
    departure({ journeyId: 'gone', status: 'Running', estimateSource: 'Confirmed' }),
    departure({ journeyId: 'next' }),
  ]
  assert.equal(platformModel(list, null).next?.journeyId, 'next')
})

test('when the next train is cancelled, the next one to the same place is offered', () => {
  const list = [
    departure({ journeyId: 'cancelled', status: 'Cancelled' }),
    departure({ journeyId: 'elsewhere', destinationCode: 'MIL', destinationName: 'Mill Lane' }),
    departure({ journeyId: 'same' }),
  ]
  const model = platformModel(list, null)
  assert.equal(model.next?.status, 'Cancelled')
  assert.equal(model.alternative?.journeyId, 'same')
})

test('no trains at all gives nothing to show', () => {
  const model = platformModel([], null)
  assert.equal(model.next, null)
  assert.deepEqual(model.after, [])
})

test('the Sinhala words cover everything the English words do', () => {
  const keys = (value: unknown, path = ''): string[] =>
    typeof value === 'object' && value !== null
      ? Object.entries(value).flatMap(([k, v]) => keys(v, `${path}${k}.`))
      : [path]
  assert.deepEqual(keys(words.si), keys(words.en))
})

test('every status is said in Sinhala, and none of them is left in English', () => {
  for (const [code, text] of Object.entries(words.si.statuses)) {
    assert.ok(/[\u0D80-\u0DFF]/.test(text), `${code} has no Sinhala letters: ${text}`)
  }
})

test('a late train is described with its time in either language', () => {
  const row = departureRow(departure({ delayMinutes: 12, estimateSource: 'Live', confidence: 'Live', expectedDepartureUtc: '2026-10-21T01:12:00Z' }), live)
  assert.equal(rowLabel('en', row), 'Expected 06:42')
  assert.equal(rowLabel('si', row), '06:42 ට අපේක්ෂිතයි')
})

test('the words with a place or a time in them put it in the right spot', () => {
  assert.equal(words.en.nextTrain('Riverside', '14:55', '3'), 'Next train to Riverside: 14:55 from platform 3')
  assert.equal(words.si.nextTrain('Riverside', '14:55', '3'), 'Riverside වෙත ඊළඟ දුම්රිය: 14:55 · වේදිකාව 3')
  assert.equal(words.en.nextTrain('Riverside', '14:55', null), 'Next train to Riverside: 14:55')
})

test('a train at the platform says so, in both languages', () => {
  const row = departureRow(departure({ atPlatform: true, status: 'Running', confidence: 'Live', estimateSource: 'Live' }), live)
  assert.deepEqual([row.status, row.code], ['on_time', 'atPlatform'])
  assert.equal(rowLabel('en', row), 'At platform')
  assert.equal(rowLabel('si', row), 'වේදිකාවේ ඇත')
})

test('a train that is at the platform is shown as there even when it got in late', () => {
  const row = departureRow(departure({ atPlatform: true, delayMinutes: 6, status: 'Running', confidence: 'Live', estimateSource: 'Live' }), live)
  assert.equal(row.code, 'atPlatform')
})

test('a train on the next day is marked as tomorrow', () => {
  // 01:00 UTC is 06:30 in Colombo. 18:55 UTC is 00:25 the next day there.
  assert.equal(departureRow(departure(), live).tomorrow, false)
  assert.equal(departureRow(departure({ scheduledDepartureUtc: '2026-10-21T18:55:00Z' }), live).tomorrow, true)
  assert.equal(arrivalRow(arrival({ scheduledArrivalUtc: '2026-10-21T19:10:00Z' }), live).tomorrow, true)
})

test('the day is the network\'s day, not the screen\'s', () => {
  assert.equal(dayOf('2026-10-21T18:55:00Z', zone), '2026-10-22')
  assert.equal(dayOf('2026-10-21T18:55:00Z', 'UTC'), '2026-10-21')
})

test('a long board is cut into pages of six, and stays up long enough to show them all', () => {
  assert.deepEqual(pagesOf([1, 2, 3, 4, 5, 6, 7, 8]).map((p) => p.length), [6, 2])
  assert.deepEqual(pagesOf([]), [])
  assert.equal(boardSeconds(0), 14)
  assert.equal(boardSeconds(6), 14)
  assert.equal(boardSeconds(12), 14)
  assert.equal(boardSeconds(13), 21)
})

test('rows that left and rows that came on are found by the train, not by their place', () => {
  const row = (key: string) => departureRow(departure({ journeyId: key }), live)
  const diff = diffRows([row('a'), row('b'), row('c')], [row('b'), row('c'), row('d')])
  assert.deepEqual(diff, { removed: ['a'], added: ['d'] })
  assert.deepEqual(diffRows([row('a')], [row('a')]), { removed: [], added: [] })
})
