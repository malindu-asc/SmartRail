/** Where the API and the sign-in service are. The API is on the same origin (proxied), so no CORS. */
export const API_CONFIG = {
  baseUrl: '/api',
} as const

export const AUTH_CONFIG = {
  url: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  realm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'smartrail',
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'smartrail-web',
} as const
