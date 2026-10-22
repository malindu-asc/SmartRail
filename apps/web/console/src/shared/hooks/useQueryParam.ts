import { useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'

/**
 * A value that lives in the URL (?status=delayed): survives a refresh and can be shared.
 * Setting the default or an empty value removes it from the address.
 */
export function useQueryParam(name: string, fallback = ''): [string, (value: string) => void] {
  const [params, setParams] = useSearchParams()
  const value = params.get(name) ?? fallback

  const set = useCallback(
    (next: string) => {
      setParams(
        (current) => {
          const copy = new URLSearchParams(current)
          if (!next || next === fallback) copy.delete(name)
          else copy.set(name, next)
          return copy
        },
        { replace: true },
      )
    },
    [fallback, name, setParams],
  )

  return [value, set]
}
