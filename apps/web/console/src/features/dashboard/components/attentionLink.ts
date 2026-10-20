import type { Attention } from '../types'

/** Where "Open", "Assign" and "Review" lead. */
export function attentionLink(item: Attention): string {
  switch (item.targetType) {
    case 'journey':
      return item.action === 'Assign' ? '/planning' : `/live-board/${item.targetId}`
    case 'assistance':
      return '/driver-messages?tab=assistance'
    case 'message':
      return '/driver-messages'
    case 'disruption':
      return `/disruptions/${item.targetId}`
    default:
      return '/dashboard'
  }
}
