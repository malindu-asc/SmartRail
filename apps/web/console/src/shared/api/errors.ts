/** A failed API call. `code` is the backend's stable error code (for example Journeys.VersionConflict). */
export class ApiError extends Error {
  readonly status: number
  readonly code: string | null

  constructor(status: number, message: string, code: string | null = null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }

  get isConflict(): boolean {
    return this.status === 409
  }

  get isForbidden(): boolean {
    return this.status === 403
  }

  get isNotFound(): boolean {
    return this.status === 404
  }
}

interface ProblemDetails {
  title?: string
  detail?: string
  code?: string
}

/** Builds an ApiError from a failed response (RFC 7807 problem details). */
export async function apiErrorFrom(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {}
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    // not JSON: keep the status text
  }

  return new ApiError(response.status, problem.detail ?? problem.title ?? `Request failed (${response.status})`, problem.code ?? null)
}

// Text for the errors a person can act on. Anything else shows the server's own sentence.
const FRIENDLY: Record<string, string> = {
  'Journeys.VersionConflict': 'Someone else changed this journey. The latest version is shown; check it and try again.',
  'Journeys.PlatformInUse': 'Another train uses that platform at that time.',
  'Journeys.DriverNotRested': 'That driver would not have the required rest.',
  'Operations.SecondApproverRequired': 'A different controller must approve this notice.',
  'Network.AuthorCannotApprove': 'The author cannot approve their own timetable. Ask another scheduler.',
  'Network.DraftAlreadyOpen': 'A draft already exists. Finish or publish it first.',
  'Identity.CannotChangeOwnAccess': 'You cannot change your own access.',
  'Identity.LastAdministrator': 'The last administrator cannot be removed.',
}

/** One sentence to show a person. */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const friendly = error.code ? FRIENDLY[error.code] : undefined
    if (friendly) return friendly
    if (error.status === 403) return 'Your role does not allow this.'
    if (error.status === 401) return 'Your session ended. Sign in again.'
    if (error.status >= 500) return 'The server had a problem. Try again in a moment.'
    return error.message
  }
  return error instanceof Error ? error.message : 'Something went wrong.'
}
