import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';
import type { AuthUser } from './auth-user';

/**
 * The seam between AuthService and the identity provider, so guards and stores
 * stay testable without a running Keycloak (spec 4.2). Implemented by
 * KeycloakAuthProvider.
 */
export interface AuthProvider {
  /**
   * Resolves the identity behind an existing single-sign-on session, or null
   * when nobody is signed in. Called once during bootstrap.
   */
  restore(): Observable<AuthUser | null>;
  /**
   * Starts the authorization-code redirect. The promise does not resolve on
   * success — the browser leaves the page.
   */
  login(returnUrl: string | null): Promise<void>;
  logout(): Promise<void>;
  /** Read per request. Never cache it: the provider rotates it on refresh. */
  token(): string | null;
}

export const AUTH_PROVIDER = new InjectionToken<AuthProvider>('AUTH_PROVIDER');
