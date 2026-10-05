import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import type { AuthProvider } from './auth-provider';
import type { AuthUser, Credentials } from './auth-user';

/**
 * Duende IdentityServer path, prepared but inert.
 *
 * Enabling it requires, in order:
 *   1. An authentication scheme on the API — Program.cs currently calls
 *      UseAuthorization() with no UseAuthentication() and no JWT bearer registered.
 *   2. A CORS policy on the API, which does not exist yet.
 *   3. `npm i angular-auth-oidc-client`, then provideAuth(...) in app.config.ts.
 *   4. environment.useOidc = true and a populated oidc.authority.
 *
 * The tenant is read from the `org_id` claim and must populate
 * AuthUser.organizationId so tenant.guard keeps working unchanged.
 */
@Injectable({ providedIn: 'root' })
export class OidcAuthProvider implements AuthProvider {
  login(_credentials: Credentials): Observable<AuthUser> {
    return throwError(() => new ApiFailure('client', 'OIDC sign-in is not configured yet.'));
  }

  logout(): Observable<void> {
    return throwError(() => new ApiFailure('client', 'OIDC sign-out is not configured yet.'));
  }

  restore(): AuthUser | null {
    return null;
  }
}
