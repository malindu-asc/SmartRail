/** A read of the public API. It gives up after a few seconds, so a dead connection is noticed instead of waited for. */
export class ApiError extends Error {
  status: number

  constructor(status: number) {
    super(`The API answered ${status}`)
    this.status = status
  }
}

export async function getJson<T>(path: string, timeoutMs = 8000): Promise<T> {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), timeoutMs)
  try {
    const response = await fetch(path, { headers: { Accept: 'application/json' }, signal: controller.signal, cache: 'no-store' })
    if (!response.ok) throw new ApiError(response.status)
    return (await response.json()) as T
  } finally {
    clearTimeout(timer)
  }
}
