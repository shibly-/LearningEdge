/**
 * Every LearningEdge endpoint wraps its payload in Result<T>, serialized camelCase.
 *
 *   { "success": true,  "error": "",        "data": { ... } }
 *   { "success": false, "error": "reason",  "data": null }
 *
 * A missing record returns HTTP 200 with success:false, NOT 404 — the controllers'
 * null check can never fire because Result<T> is always a live instance. Branch on
 * `success`, never on the status code.
 */
export interface ApiResult<T> {
  readonly success: boolean;
  readonly error: string;
  readonly data: T | null;
}

export type ApiFailureKind =
  'envelope' | 'not-found' | 'offline' | 'rate-limited' | 'server' | 'client' | 'malformed';

/** The single error type feature code is allowed to see. */
export class ApiFailure extends Error {
  constructor(
    readonly kind: ApiFailureKind,
    message: string,
    readonly status = 0,
  ) {
    super(message);
    this.name = 'ApiFailure';
  }
}

export function isApiResult<T>(value: unknown): value is ApiResult<T> {
  if (value === null || typeof value !== 'object') {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return typeof candidate['success'] === 'boolean' && 'data' in candidate;
}

/**
 * Throws when the envelope reports failure or carries no payload.
 * Use unwrapOptional when an absent record is a legitimate outcome.
 */
export function unwrap<T>(result: unknown): T {
  if (!isApiResult<T>(result)) {
    throw new ApiFailure('malformed', 'The server returned an unexpected response shape.');
  }
  if (!result.success) {
    throw new ApiFailure('envelope', result.error || 'The request was rejected by the server.');
  }
  if (result.data === null || result.data === undefined) {
    throw new ApiFailure('not-found', result.error || 'The requested record was not found.');
  }
  return result.data;
}

/** Returns null instead of throwing when the record is simply absent. */
export function unwrapOptional<T>(result: unknown): T | null {
  if (!isApiResult<T>(result)) {
    throw new ApiFailure('malformed', 'The server returned an unexpected response shape.');
  }
  if (!result.success) {
    return null;
  }
  return result.data ?? null;
}
