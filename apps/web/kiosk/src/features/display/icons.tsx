import type { ReactNode } from 'react'

const paths: Record<string, ReactNode> = {
  check: <path d="M5 12.5l4.5 4.5L19 7.5" />,
  'check-circle': (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M8 12.5l3 3 5-6" />
    </>
  ),
  clock: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5.5l3.5 2" />
    </>
  ),
  'x-circle': (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M9 9l6 6M15 9l-6 6" />
    </>
  ),
  'arrow-lr': <path d="M4 8h15m-4-4l4 4-4 4M20 16H5m4 4l-4-4 4-4" />,
  'alert-triangle': (
    <>
      <path d="M12 3.5l9.5 16.5h-19z" />
      <path d="M12 10v4.5M12 17.5v.01" />
    </>
  ),
  'wifi-off': (
    <>
      <path d="M3 3l18 18" />
      <path d="M2 9a15 15 0 0 1 6-3.4M22 9a15 15 0 0 0-8.5-4M5.5 12.5a10 10 0 0 1 3.6-2M18.5 12.5a10 10 0 0 0-3.2-1.9M9 16a5 5 0 0 1 5.8-.8" />
      <path d="M12 19.5v.01" />
    </>
  ),
  volume: (
    <>
      <path d="M4 9.5v5h3.5L12 18.5v-13L7.5 9.5z" />
      <path d="M15.5 9a4 4 0 0 1 0 6M18 6.5a8 8 0 0 1 0 11" />
    </>
  ),
  lock: (
    <>
      <rect x="5" y="11" width="14" height="9" rx="2" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" />
    </>
  ),
  accessible: (
    <>
      <circle cx="12" cy="4.5" r="1.8" />
      <path d="M12 8v6h4.5l2 5M12 11h5M9 12.5a4.5 4.5 0 1 0 5.5 6.5" />
    </>
  ),
}

export type IconName = keyof typeof paths

/** Line icons drawn on a 24 grid. They take the colour of the text around them. */
export function Icon({ name, size = 32 }: { name: IconName; size?: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className="icon"
    >
      {paths[name]}
    </svg>
  )
}
