import { clsx, type ClassValue } from 'clsx'

/** Merges conditional class names. */
export function cn(...inputs: ClassValue[]): string {
  return clsx(inputs)
}
