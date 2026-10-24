import { cn } from '@/shared/utils'

interface LogoProps {
  /** "mark" is the train alone (sidebars, small spaces); "full" adds the SMARTRAIL wordmark (start and sign-in screens). */
  variant?: 'mark' | 'full'
  className?: string
}

/** The SmartRail logo, from public/. The alt text is the product name so screen readers say it once. */
export function Logo({ variant = 'mark', className }: LogoProps) {
  return (
    <img
      src={variant === 'full' ? '/logo.png' : '/logo-mark.png'}
      alt="SmartRail"
      className={cn(variant === 'full' ? 'h-24 w-auto' : 'h-8 w-auto', className)}
      draggable={false}
    />
  )
}
