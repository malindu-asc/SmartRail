import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  // In development the API is on another port; in the container nginx puts it on the same address.
  server: { proxy: { '/api': process.env.VITE_API_PROXY ?? 'http://localhost:5080' } },
})
