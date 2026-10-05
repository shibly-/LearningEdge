import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';
import type { AuthUser, Credentials } from './auth-user';

/**
 * Implemented twice: MockAuthProvider (the only working path today) and
 * OidcAuthProvider. app.config.ts selects one by environment flag.
 */
export interface AuthProvider {
  login(credentials: Credentials): Observable<AuthUser>;
  logout(): Observable<void>;
  /** Rehydrates a persisted session on bootstrap, or null. */
  restore(): AuthUser | null;
}

export const AUTH_PROVIDER = new InjectionToken<AuthProvider>('AUTH_PROVIDER');
