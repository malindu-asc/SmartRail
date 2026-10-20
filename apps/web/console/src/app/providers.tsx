import { QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { Toaster } from 'react-hot-toast'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '@/shared/auth'
import { createQueryClient } from './queryClient'

/** Everything the screens depend on, in one place: data cache, sign-in, routing and toasts. */
export function Providers({ children }: { children: ReactNode }) {
  const [queryClient] = useState(createQueryClient)

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>{children}</AuthProvider>
      </BrowserRouter>
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 4000,
          style: { background: 'var(--sr-surface)', color: 'var(--sr-fg)', border: '1px solid var(--sr-border)', borderRadius: 'var(--sr-radius-md)', fontSize: '14px' },
          success: { iconTheme: { primary: 'var(--sr-on-time)', secondary: 'var(--sr-surface)' } },
          error: { duration: 6000, iconTheme: { primary: 'var(--sr-cancelled)', secondary: 'var(--sr-surface)' } },
        }}
      />
    </QueryClientProvider>
  )
}
