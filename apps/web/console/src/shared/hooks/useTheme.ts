import { useCallback, useState } from 'react'

export type Theme = 'light' | 'dark'

const KEY = 'smartrail-console-theme'

function readTheme(): Theme {
  try {
    const stored = localStorage.getItem(KEY)
    if (stored === 'light' || stored === 'dark') return stored
  } catch {
    // storage blocked: fall through
  }
  // Light is the default, whatever the operating system prefers. Dark is opt-in with the toggle.
  return 'light'
}

function apply(theme: Theme): void {
  document.documentElement.dataset.theme = theme
}

/** Light or dark, remembered on this browser. Both come from the same tokens. */
export function useTheme(): { theme: Theme; toggle: () => void } {
  const [theme, setTheme] = useState<Theme>(() => {
    const initial = readTheme()
    apply(initial)
    return initial
  })

  const toggle = useCallback(() => {
    setTheme((current) => {
      const next = current === 'dark' ? 'light' : 'dark'
      apply(next)
      try {
        localStorage.setItem(KEY, next)
      } catch {
        // storage blocked: the choice lasts for this page only
      }
      return next
    })
  }, [])

  return { theme, toggle }
}
