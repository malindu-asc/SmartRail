import { cva, type VariantProps } from 'class-variance-authority'

// Where "what does a danger button look like" lives: one place, token classes only.
export const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-control font-semibold transition-colors disabled:opacity-50',
  {
    variants: {
      variant: {
        primary: 'bg-primary text-on-primary hover:bg-primary-pressed',
        secondary: 'border border-line-strong bg-surface text-fg hover:bg-soft',
        ghost: 'text-fg-secondary hover:bg-soft hover:text-fg',
        danger: 'bg-cancelled text-surface hover:opacity-90',
        subtle: 'bg-primary-subtle text-primary hover:opacity-90',
      },
      size: {
        sm: 'h-8 px-3 text-[13px]',
        md: 'h-10 px-4 text-sm',
        lg: 'h-11 px-5 text-sm',
      },
    },
    defaultVariants: { variant: 'primary', size: 'md' },
  },
)

export type ButtonVariants = VariantProps<typeof buttonVariants>
