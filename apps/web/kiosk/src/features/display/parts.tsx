import type { ReactNode } from 'react'
import { Icon, type IconName } from './icons'

/** What a train is doing. Always an icon, words and a colour, never colour alone. */
export type Status = 'on_time' | 'delayed' | 'cancelled' | 'changed' | 'scheduled'

const statusIcon: Record<Status, IconName> = {
  on_time: 'check',
  delayed: 'clock',
  cancelled: 'x-circle',
  changed: 'arrow-lr',
  scheduled: 'clock',
}

export function StatusBadge({
  status,
  label,
  size = 'md',
}: {
  status: Status
  label: string
  size?: 'sm' | 'md' | 'lg'
}) {
  const px = size === 'lg' ? 36 : size === 'md' ? 28 : 24
  return (
    <span className={`status status-${status} status-${size}`}>
      <Icon name={statusIcon[status]} size={px} />
      {label}
    </span>
  )
}

/** The top strip: the place on the left, the network time on the right. */
export function Header({ title, time }: { title: string; time: string }) {
  return (
    <header className="d-header">
      <h1>{title}</h1>
      <time>{time}</time>
    </header>
  )
}

/** The bottom strip: whether the information is live, or the notice that matters most. */
export function Footer({
  tone,
  icon,
  children,
  right,
}: {
  tone: 'live' | 'alert'
  icon: IconName
  children: ReactNode
  right?: ReactNode
}) {
  return (
    <footer className={`d-footer d-footer-${tone}`}>
      <Icon name={icon} size={26} />
      <span>{children}</span>
      {right && <span className="d-footer-right">{right}</span>}
    </footer>
  )
}

/** A full-width notice under the header, used for a platform change. */
export function Banner({ icon, children }: { icon: IconName; children: ReactNode }) {
  return (
    <div className="d-banner">
      <Icon name={icon} size={44} />
      <span>{children}</span>
    </div>
  )
}

/** The screen itself: a fixed 1280 by 720 stage that the page scales to fit. */
export function Stage({ children }: { children: ReactNode }) {
  return <div className="d-stage">{children}</div>
}
