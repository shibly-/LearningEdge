import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiFailure } from '../http/api-result';
import { ApiIdentityResolver } from './api-identity.resolver';
import type { AuthProvider } from './auth-provider';
import type { AuthUser, Credentials } from './auth-user';
import { findMockUser, mockCredentials } from './mock-users';
import { SessionStore } from './session-storage';

/**
 * The only functional auth path today — the API registers no authentication
 * scheme, so there is no token to obtain (spec 3.9).
 *
 * With the live API the configured login is mapped onto the API user with the
 * same email; with mock data the configured identity is used as-is.
 */
@Injectable({ providedIn: 'root' })
export class MockAuthProvider implements AuthProvider {
  private readonly session = inject(SessionStore);
  private readonly resolver = inject(ApiIdentityResolver);

  login(credentials: Credentials): Observable<AuthUser> {
    if (mockCredentials().length === 0) {
      return throwError(
        () =>
          new ApiFailure(
            'client',
            'No mock logins are configured. Run `npm run setup:env` and restart the dev server.',
          ),
      );
    }

    const user = findMockUser(credentials.username, credentials.password);
    if (user === null) {
      return throwError(() => new ApiFailure('client', 'Incorrect username or password.'));
    }

    // A small delay keeps the submitting/disabled states exercised in dev.
    const identity = environment.useMockApi
      ? of(user).pipe(delay(250))
      : this.resolver.resolve(user);
    return identity.pipe(tap((resolved) => this.session.write(resolved)));
  }

  logout(): Observable<void> {
    this.session.clear();
    return of(undefined);
  }

  restore(): AuthUser | null {
    return this.session.read();
  }
}
