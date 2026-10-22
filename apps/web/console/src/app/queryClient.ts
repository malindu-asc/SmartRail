import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/shared/api'

/** One shared cache. A forbidden or missing answer will not change by asking again, so those are not retried. */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 10_000,
        refetchOnWindowFocus: true,
        retry: (failureCount, error) => !(error instanceof ApiError && error.status >= 400 && error.status < 500) && failureCount < 2,
      },
    },
  })
}
