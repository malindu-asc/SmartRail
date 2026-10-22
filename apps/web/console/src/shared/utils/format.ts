// The API sends UTC. People read the network's local time (Asia/Colombo), so formatting lives in one place.

export const NETWORK_TIME_ZONE = 'Asia/Colombo'

const timeFormat = new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: NETWORK_TIME_ZONE })
const dateFormat = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', timeZone: NETWORK_TIME_ZONE })
const dateTimeFormat = new Intl.DateTimeFormat('en-GB', {
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
  timeZone: NETWORK_TIME_ZONE,
})
const isoDateFormat = new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: NETWORK_TIME_ZONE })

function toDate(value: string | Date | null | undefined): Date | null {
  if (!value) return null
  const date = value instanceof Date ? value : new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

/** 14:25 */
export function formatTime(value: string | Date | null | undefined): string {
  const date = toDate(value)
  return date ? timeFormat.format(date) : '–'
}

/** 14 Oct */
export function formatDate(value: string | Date | null | undefined): string {
  const date = toDate(value)
  return date ? dateFormat.format(date) : '–'
}

/** 14 Oct, 14:25 */
export function formatDateTime(value: string | Date | null | undefined): string {
  const date = toDate(value)
  return date ? dateTimeFormat.format(date) : '–'
}

/** Today's service date as yyyy-MM-dd in the network time zone. */
export function todayIso(now: Date = new Date()): string {
  return isoDateFormat.format(now)
}

/** "just now", "5 min ago", "3 h ago", "2 d ago" */
export function formatAgo(value: string | Date | null | undefined, now: Date = new Date()): string {
  const date = toDate(value)
  if (!date) return '–'
  const minutes = Math.floor((now.getTime() - date.getTime()) / 60_000)
  if (minutes < 1) return 'just now'
  if (minutes < 60) return `${minutes} min ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 48) return `${hours} h ago`
  return `${Math.floor(hours / 24)} d ago`
}

/** Seconds as "45 s" or "3 min". */
export function formatAge(seconds: number | null | undefined): string {
  if (seconds === null || seconds === undefined) return '–'
  if (seconds < 90) return `${seconds} s`
  return `${Math.round(seconds / 60)} min`
}

/** +7, 0, – */
export function formatDelay(minutes: number | null | undefined): string {
  if (minutes === null || minutes === undefined) return '–'
  return minutes > 0 ? `+${minutes}` : String(minutes)
}

/** "SP" from "Sarah Perera" */
export function initials(name: string | null | undefined): string {
  if (!name) return '?'
  const parts = name.trim().split(/\s+/)
  const first = parts[0]?.[0] ?? ''
  const last = parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? '') : ''
  return (first + last).toUpperCase() || '?'
}

/** "signal_fault" to "Signal fault" */
export function humanize(code: string): string {
  const text = code
    .replace(/[_.]+/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .trim()
    .toLowerCase()
  return text.charAt(0).toUpperCase() + text.slice(1)
}

/** "HH:mm" (hours may pass 24) to minutes after midnight; null when empty or malformed. */
export function parseClock(text: string): number | null {
  const match = /^(\d{1,2}):([0-5]\d)$/.exec(text.trim())
  return match ? Number(match[1]) * 60 + Number(match[2]) : null
}

/** The value of a <input type="datetime-local"> (this browser's local time) as a UTC instant for the API. */
export function localInputToUtc(value: string): string {
  return new Date(value).toISOString()
}

/** A UTC instant (or now plus minutes) as the value a <input type="datetime-local"> expects. */
export function toLocalInput(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}
