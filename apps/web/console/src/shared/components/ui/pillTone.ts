import type { PillTone } from './StatusPill'

/** The server sends `statusKind` and similar words; this maps them to a tone in one place. */
export function toneFor(kind: string | null | undefined): PillTone {
  switch ((kind ?? '').toLowerCase()) {
    case 'ontime':
    case 'on time':
    case 'online':
    case 'active':
    case 'live':
    case 'published':
    case 'acknowledged':
    case 'resolved':
    case 'good':
    case 'ok':
      return 'ontime'
    case 'delayed':
    case 'late':
    case 'warning':
    case 'awaitingapproval':
    case 'draft':
    case 'major':
    case 'sent':
    case 'leaver':
    case 'minor':
      return 'delayed'
    case 'cancelled':
    case 'offline':
    case 'locked':
    case 'severe':
    case 'critical':
    case 'rejected':
    case 'revoked':
    case 'bad':
    case 'danger':
      return 'cancelled'
    case 'changed':
    case 'platformchanged':
    case 'info':
    case 'delivered':
      return 'changed'
    case 'stale':
    case 'unknown':
    case 'scheduled':
    case 'pending':
    case 'disabled':
    case 'retracted':
      return 'stale'
    default:
      return 'neutral'
  }
}
