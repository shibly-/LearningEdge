import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { ApiFailure, unwrap, unwrapOptional } from './api-result';
import type { ApiResult } from './api-result';

/**
 * The only place in the app that knows about the ApiResult envelope.
 * Feature code must never read `.data` or `.success` directly.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  get<T>(url: string): Observable<T> {
    return this.http.get<ApiResult<T>>(url).pipe(
      map((result) => unwrap<T>(result)),
      catchError(toApiFailure),
    );
  }

  /** Resolves to null when the record is absent (success:false on HTTP 200). */
  getOptional<T>(url: string): Observable<T | null> {
    return this.http.get<ApiResult<T>>(url).pipe(
      map((result) => unwrapOptional<T>(result)),
      catchError(toApiFailure),
    );
  }

  /**
   * POST endpoints return ApiResult<string> holding only the new GUID — not the
   * created DTO, despite the controllers declaring ActionResult<UserDTO>.
   * Re-fetch by id if the full record is needed.
   */
  post<TBody>(url: string, body: TBody): Observable<string> {
    return this.http.post<ApiResult<string>>(url, body).pipe(
      map((result) => unwrap<string>(result)),
      catchError(toApiFailure),
    );
  }
}

function toApiFailure(error: unknown): Observable<never> {
  if (error instanceof ApiFailure) {
    return throwError(() => error);
  }

  if (error instanceof HttpErrorResponse) {
    // Status 0 means the request never completed: the API is down, HTTPS was
    // rejected, or — most likely here — no CORS policy is configured (spec 3.9).
    if (error.status === 0) {
      return throwError(
        () =>
          new ApiFailure(
            'offline',
            'Cannot reach the server. Check that the API is running and that CORS is configured.',
            0,
          ),
      );
    }

    // The fixed-window limiter rejects with 503, not 429, because
    // RejectionStatusCode is left at the framework default.
    if (error.status === 503) {
      return throwError(
        () => new ApiFailure('rate-limited', 'The server is busy. Please try again shortly.', 503),
      );
    }

    if (error.status >= 500) {
      return throwError(
        () => new ApiFailure('server', 'Something went wrong. Please try again.', error.status),
      );
    }

    return throwError(
      () =>
        new ApiFailure(
          'client',
          readServerMessage(error) ?? 'The request was rejected.',
          error.status,
        ),
    );
  }

  return throwError(() => new ApiFailure('malformed', 'An unexpected error occurred.'));
}

function readServerMessage(error: HttpErrorResponse): string | null {
  const body: unknown = error.error;
  if (body !== null && typeof body === 'object') {
    const record = body as Record<string, unknown>;
    for (const key of ['error', 'title', 'detail', 'message']) {
      const value = record[key];
      if (typeof value === 'string' && value.length > 0) {
        return value;
      }
    }
  }
  return null;
}
