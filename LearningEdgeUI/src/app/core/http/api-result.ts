/**
 * The API returns plain DTOs on success. Failures carry an RFC 7807
 * ProblemDetails body and a meaningful status code:
 *
 *   400 validation · 403 forbidden · 404 not found · 409 conflict · 500 server
 *
 * Feature code only ever sees ApiFailure and branches on `kind`, never on raw
 * status codes.
 */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
}

export type ApiFailureKind =
  | 'validation'
  | 'forbidden'
  | 'not-found'
  | 'conflict'
  | 'offline'
  | 'rate-limited'
  | 'server'
  | 'client'
  | 'malformed';

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

export function failureKindForStatus(status: number): ApiFailureKind {
  switch (status) {
    case 0:
      return 'offline';
    case 400:
      return 'validation';
    case 403:
      return 'forbidden';
    case 404:
      return 'not-found';
    case 409:
      return 'conflict';
    // The fixed-window limiter rejects with 503 because RejectionStatusCode is
    // left at the framework default.
    case 503:
      return 'rate-limited';
    default:
      return status >= 500 ? 'server' : 'client';
  }
}

/**
 * Picks the most useful human message from an error body: ProblemDetails
 * `detail`, then `title`, then ASP.NET's validation `errors` dictionary.
 */
export function readProblemMessage(body: unknown): string | null {
  if (typeof body === 'string' && body.trim().length > 0) {
    return body.trim();
  }
  if (body === null || typeof body !== 'object') {
    return null;
  }

  const record = body as Record<string, unknown>;
  const detail = record['detail'];
  if (typeof detail === 'string' && detail.length > 0) {
    return detail;
  }

  const errors = record['errors'];
  if (errors !== null && typeof errors === 'object') {
    const messages = Object.values(errors as Record<string, unknown>)
      .flatMap((value) => (Array.isArray(value) ? value : [value]))
      .filter((value): value is string => typeof value === 'string' && value.length > 0);
    if (messages.length > 0) {
      return messages.join(' ');
    }
  }

  const title = record['title'];
  return typeof title === 'string' && title.length > 0 ? title : null;
}
