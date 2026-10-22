import toast from 'react-hot-toast'
import { errorMessage } from '@/shared/api'

/**
 * One way to tell a person what happened. One toast per action, in words they can act on.
 * (Form validation goes next to the field, not here.)
 */
export const notify = {
  success(message: string): void {
    toast.success(message)
  },
  error(error: unknown, fallback?: string): void {
    const message = errorMessage(error)
    toast.error(fallback ? `${fallback} ${message}` : message)
  },
  info(message: string): void {
    toast(message)
  },
}
