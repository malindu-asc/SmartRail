import { useMutation, useQueryClient, type QueryKey } from '@tanstack/react-query'
import { ApiError } from '@/shared/api'
import { notify } from '@/shared/components/ui'

interface ActionOptions<TVariables, TResult> {
  mutationFn: (variables: TVariables) => Promise<TResult>
  /** Shown as a toast when it worked. May use the result. */
  successMessage: string | ((result: TResult, variables: TVariables) => string)
  /** Query keys to refresh afterwards (a prefix refreshes every key under it). */
  invalidate?: QueryKey[]
  /** Text put before the server's sentence when it fails: "Could not cancel the journey." */
  failureMessage?: string
  onSuccess?: (result: TResult, variables: TVariables) => void
}

/**
 * A server action (cancel, approve, invite ...): runs the call, toasts the outcome and refreshes what changed.
 * After a conflict (someone else got there first) it refreshes too, so the screen shows the latest state.
 */
export function useAction<TVariables = void, TResult = void>(options: ActionOptions<TVariables, TResult>) {
  const queryClient = useQueryClient()
  const refresh = () => Promise.all((options.invalidate ?? []).map((queryKey) => queryClient.invalidateQueries({ queryKey })))

  return useMutation({
    mutationFn: options.mutationFn,
    onSuccess: async (result, variables) => {
      await refresh()
      const message = typeof options.successMessage === 'function' ? options.successMessage(result, variables) : options.successMessage
      notify.success(message)
      options.onSuccess?.(result, variables)
    },
    onError: async (error) => {
      if (error instanceof ApiError && error.isConflict) await refresh()
      notify.error(error, options.failureMessage)
    },
  })
}
