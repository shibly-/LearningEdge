import { HttpErrorResponse } from '@angular/common/http';
import type { HttpInterceptorFn } from '@angular/common/http';
import { retry, timer, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_API_CONCERNS } from './http-context';

/**
 * GET /api/v1/user/{id} is limited to 5 requests per 10 seconds with a queue of 2.
 * Rejections arrive as HTTP 503, not 429, because RejectionStatusCode is not
 * configured server-side. No Retry-After header is sent, so back off on our own
 * schedule with jitter to avoid a synchronized retry storm.
 */
export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'GET' || req.context.get(SKIP_API_CONCERNS)) {
    return next(req);
  }

  const { maxAttempts, baseDelayMs, maxDelayMs } = environment.rateLimitRetry;

  return next(req).pipe(
    retry({
      count: maxAttempts,
      delay: (error, attempt) => {
        if (!(error instanceof HttpErrorResponse) || error.status !== 503) {
          return throwError(() => error);
        }
        const exponential = Math.min(baseDelayMs * 2 ** (attempt - 1), maxDelayMs);
        const jitter = Math.random() * baseDelayMs;
        return timer(exponential + jitter);
      },
    }),
  );
};
