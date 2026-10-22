import type { APIRequestContext } from '@playwright/test'
import { account, type Persona } from './accounts'

/** Keycloak and the API as the browser reaches them on the Compose stack. */
const KEYCLOAK = process.env.KEYCLOAK_URL ?? 'http://localhost:8080'
const API = process.env.API_URL ?? 'http://localhost:5080'

/**
 * A bearer token for a seeded person, by the password grant of the development client. It stands in for the driver's phone,
 * which has no browser: the driver app calls the same endpoints.
 */
export async function tokenFor(request: APIRequestContext, persona: Persona): Promise<string> {
  const { email, password } = account(persona)
  const response = await request.post(`${KEYCLOAK}/realms/smartrail/protocol/openid-connect/token`, {
    form: { grant_type: 'password', client_id: 'smartrail-dev-token', username: email, password },
  })
  if (!response.ok()) throw new Error(`Could not sign in ${persona}: ${response.status()}`)
  return (await response.json()).access_token as string
}

export function authorised(token: string) {
  return { headers: { Authorization: `Bearer ${token}` } }
}

/** The notices passengers can read: public, no token. */
export async function publicDisruptions(request: APIRequestContext): Promise<{ title: string }[]> {
  const response = await request.get(`${API}/api/operations/disruptions`)
  return (await response.json()) as { title: string }[]
}

export const api = (path: string) => `${API}${path}`
