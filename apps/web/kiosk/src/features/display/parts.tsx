import type { ReactNode } from 'react'
import { Icon, type IconName } from './icons'

/** What a train is doing. Always an icon, words and a colour, never colour alone. */
export type Status = 'on_time' | 'delayed' | 'cancelled' | 'changed' | 'scheduled'

export function StatusBadge({
  status,
  label,
  sub,
  size = 'md',
}: {
  status: Status
  label: string
  /** The same words in the second language, in a smaller line under the first. */
  sub?: string
  size?: 'sm' | 'md' | 'lg'
}) {
  return (
    <span className={`status status-${status} status-${size}`}>
      {sub ? (
        <span className="status-text">
          <span>{label}</span>
          <small className="status-sub">{sub}</small>
        </span>
      ) : (
        label
      )}
    </span>
  )
}

/** The top strip: the place on the left, the network time on the right. */
export function Header({ title, sub, time }: { title: string; sub?: string; time: string }) {
  return (
    <header className="d-header">
      <h1>
        {title}
        {sub && <span className="d-sub">{sub}</span>}
      </h1>
      <time>{time}</time>
    </header>
  )
}

/** The bottom strip: whether the information is live, or the notice that matters most. */
export function Footer({
  tone,
  icon,
  children,
  sub,
  right,
}: {
  tone: 'live' | 'alert' | 'stale'
  icon: IconName
  children: ReactNode
  /** The same words in the second language, in a smaller line under the first. */
  sub?: ReactNode
  right?: ReactNode
}) {
  return (
    <footer className={`d-footer d-footer-${tone} ${sub ? 'd-footer-two' : ''}`}>
      <Icon name={icon} size={26} />
      <span className="d-footer-text">
        <span>{children}</span>
        {sub && <small className="d-footer-sub">{sub}</small>}
      </span>
      {right && <span className="d-footer-right">{right}</span>}
    </footer>
  )
}

/** A full-width notice under the header, used for a platform change. */
export function Banner({
  icon,
  children,
  sub,
  tone = 'warn',
}: {
  icon: IconName
  children: ReactNode
  /** The same words in the second language, in a smaller line under the first. */
  sub?: ReactNode
  tone?: 'warn' | 'stale'
}) {
  return (
    <div className={`d-banner d-banner-${tone} ${sub ? 'd-banner-two' : ''}`}>
      <Icon name={icon} size={sub ? 40 : 44} />
      <span className="d-banner-text">
        <span>{children}</span>
        {sub && <small className="d-banner-sub">{sub}</small>}
      </span>
    </div>
  )
}

/** The screen itself: a fixed 1280 by 720 stage that the page scales to fit. */
export function Stage({ children }: { children: ReactNode }) {
  return <div className="d-stage">{children}</div>
}
