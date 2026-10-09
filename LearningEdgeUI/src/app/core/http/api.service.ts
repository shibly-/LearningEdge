import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { ApiFailure, failureKindForStatus, readProblemMessage } from './api-result';

/**
 * The only place in the app that turns HTTP errors into ApiFailure.
 * Repositories call these helpers; feature code never touches HttpClient.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  get<T>(url: string): Observable<T> {
    return this.http.get<T>(url).pipe(catchError(toApiFailure));
  }

  /** Resolves to null on 404, so "not found" can be rendered as a state rather than an error. */
  getOptional<T>(url: string): Observable<T | null> {
    return this.http
      .get<T>(url)
      .pipe(
        catchError((error: unknown) =>
          error instanceof HttpErrorResponse && error.status === 404
            ? of(null)
            : toApiFailure(error),
        ),
      );
  }

  /** Create endpoints return 201 with the new id as a JSON string. */
  post<TBody>(url: string, body: TBody): Observable<string> {
    return this.http.post<unknown>(url, body).pipe(map(readCreatedId), catchError(toApiFailure));
  }

  put<TBody, TResult>(url: string, body: TBody): Observable<TResult> {
    return this.http.put<TResult>(url, body).pipe(catchError(toApiFailure));
  }

  /** Multipart upload. The browser sets the boundary, so no Content-Type header is added. */
  postForm<TResult>(url: string, form: FormData): Observable<TResult> {
    return this.http.post<TResult>(url, form).pipe(catchError(toApiFailure));
  }

  /** Delete endpoints return 204 with an empty body. */
  delete(url: string): Observable<void> {
    return this.http
      .delete(url, { observe: 'response', responseType: 'text' })
      .pipe(
        map(() => undefined),
        catchError(toApiFailure),
      );
  }
}

function readCreatedId(body: unknown): string {
  if (typeof body === 'string' && body.length > 0) {
    return body;
  }
  throw new ApiFailure('malformed', 'The server did not return the id of the new record.');
}

const FALLBACK_MESSAGES: Readonly<Record<string, string>> = {
  offline:
    'Cannot reach the server. Check that the API is running and that CORS allows this origin.',
  'rate-limited': 'The server is busy. Please try again shortly.',
  server: 'Something went wrong. Please try again.',
  forbidden: 'You do not have permission to do that.',
  'not-found': 'The requested record was not found.',
  conflict: 'That conflicts with an existing record.',
  validation: 'Some of the values are not valid.',
  client: 'The request was rejected.',
};

function toApiFailure(error: unknown): Observable<never> {
  if (error instanceof ApiFailure) {
    return throwError(() => error);
  }

  if (error instanceof HttpErrorResponse) {
    const kind = failureKindForStatus(error.status);
    // Server-side 5xx details are not meant for end users.
    const serverMessage =
      kind === 'server' || kind === 'offline' ? null : readProblemMessage(error.error);
    return throwError(
      () => new ApiFailure(kind, serverMessage ?? FALLBACK_MESSAGES[kind], error.status),
    );
  }

  return throwError(() => new ApiFailure('malformed', 'An unexpected error occurred.'));
}
