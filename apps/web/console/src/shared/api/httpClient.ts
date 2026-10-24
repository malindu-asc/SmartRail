import { API_CONFIG } from './config'
import { apiErrorFrom } from './errors'

type TokenProvider = () => Promise<string | undefined>

let tokenProvider: TokenProvider = async () => undefined
let unauthorizedHandler: () => void = () => {}

/** The auth layer plugs the signed-in token in here, so features never touch tokens. */
export function configureHttpClient(options: { getToken: TokenProvider; onUnauthorized: () => void }): void {
  tokenProvider = options.getToken
  unauthorizedHandler = options.onUnauthorized
}

export type Query = Record<string, string | number | boolean | null | undefined>

function withQuery(path: string, query?: Query): string {
  if (!query) return path
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== '') params.set(key, String(value))
  }
  const text = params.toString()
  return text ? `${path}?${text}` : path
}

async function send(method: string, path: string, body?: unknown, query?: Query): Promise<Response> {
  const headers = new Headers({ Accept: 'application/json' })
  const token = await tokenProvider()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (body !== undefined) headers.set('Content-Type', 'application/json')

  const response = await fetch(`${API_CONFIG.baseUrl}${withQuery(path, query)}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (response.status === 401) unauthorizedHandler()
  if (!response.ok) throw await apiErrorFrom(response)
  return response
}

async function json<T>(response: Response): Promise<T> {
  if (response.status === 204) return undefined as T
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

/** Typed client. Components never call fetch; features wrap these in query and mutation hooks. */
export const api = {
  async get<T>(path: string, query?: Query): Promise<T> {
    return json<T>(await send('GET', path, undefined, query))
  },
  async post<T = void>(path: string, body?: unknown): Promise<T> {
    return json<T>(await send('POST', path, body ?? {}))
  },
  async put<T = void>(path: string, body?: unknown): Promise<T> {
    return json<T>(await send('PUT', path, body ?? {}))
  },
  async patch<T = void>(path: string, body?: unknown): Promise<T> {
    return json<T>(await send('PATCH', path, body ?? {}))
  },
  async delete<T = void>(path: string): Promise<T> {
    return json<T>(await send('DELETE', path))
  },
  /** For a CSV download. */
  async text(path: string, query?: Query): Promise<string> {
    return (await send('GET', path, undefined, query)).text()
  },
}

export type Api = typeof api
