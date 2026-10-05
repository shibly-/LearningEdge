import { HttpContextToken } from '@angular/common/http';

/**
 * Marks a request as a plain static asset rather than an API call, so the
 * interceptors skip tenant headers and rate-limit retries.
 */
export const SKIP_API_CONCERNS = new HttpContextToken<boolean>(() => false);
