import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, errorMessage } from './errors'
import { api, configureHttpClient } from './httpClient'

function respond(status: number, body?: unknown) {
  const calls: { url: string; init: RequestInit }[] = []
  globalThis.fetch = (async (url: RequestInfo | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init: init ?? {} })
    return new Response(body === undefined ? null : JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch
  return calls
}

afterEach(() => configureHttpClient({ getToken: async () => undefined, onUnauthorized: () => {} }))

describe('http client', () => {
  it('sends the bearer token and builds the query string, leaving out empty values', async () => {
    configureHttpClient({ getToken: async () => 'abc', onUnauthorized: () => {} })
    const calls = respond(200, { ok: true })

    await api.get('/ops/journeys', { date: '2026-10-14', route: '', attention: true, q: undefined })

    expect(calls[0]?.url).toBe('/api/ops/journeys?date=2026-10-14&attention=true')
    expect(new Headers(calls[0]?.init.headers).get('Authorization')).toBe('Bearer abc')
  })

  it('posts JSON', async () => {
    const calls = respond(200, { id: 1 })
    await api.post('/ops/messages', { body: 'Hold' })
    expect(calls[0]?.init.method).toBe('POST')
    expect(calls[0]?.init.body).toBe('{"body":"Hold"}')
  })

  it('returns nothing for 204', async () => {
    respond(204)
    await expect(api.post('/ops/assistance/1/handle')).resolves.toBeUndefined()
  })

  it('throws an ApiError with the status and the stable code', async () => {
    respond(409, { title: 'Conflict', detail: 'The journey changed.', code: 'Journeys.VersionConflict' })
    const error = await api.post('/ops/journeys/1/cancel', {}).catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(409)
    expect((error as ApiError).code).toBe('Journeys.VersionConflict')
    expect((error as ApiError).isConflict).toBe(true)
  })

  it('calls the unauthorized handler on 401', async () => {
    const onUnauthorized = vi.fn()
    configureHttpClient({ getToken: async () => undefined, onUnauthorized })
    respond(401, { title: 'Unauthorized' })
    await api.get('/me').catch(() => undefined)
    expect(onUnauthorized).toHaveBeenCalledOnce()
  })
})

describe('error messages', () => {
  it('uses plain words for the errors a person can act on', () => {
    expect(errorMessage(new ApiError(409, 'x', 'Journeys.VersionConflict'))).toMatch(/Someone else changed/)
    expect(errorMessage(new ApiError(403, 'x'))).toMatch(/role does not allow/)
    expect(errorMessage(new ApiError(500, 'x'))).toMatch(/server had a problem/)
  })

  it("shows the server's own sentence for other client errors", () => {
    expect(errorMessage(new ApiError(400, 'Choose a reason.', 'Journeys.ReasonRequired'))).toBe('Choose a reason.')
    expect(errorMessage(new Error('boom'))).toBe('boom')
  })
})
