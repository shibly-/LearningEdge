import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import type { AuthProvider } from './auth-provider';
import type { AuthUser, Credentials } from './auth-user';
import { findMockUser, mockCredentials } from './mock-users';
import { SessionStore } from './session-storage';

/**
 * The only functional auth path today — the API registers no authentication
 * scheme, so there is no token to obtain (spec 3.9).
 */
@Injectable({ providedIn: 'root' })
export class MockAuthProvider implements AuthProvider {
  private readonly session = inject(SessionStore);

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

    this.session.write(user);
    // A small delay keeps the submitting/disabled states exercised in dev.
    return of(user).pipe(delay(250));
  }

  logout(): Observable<void> {
    this.session.clear();
    return of(undefined);
  }

  restore(): AuthUser | null {
    return this.session.read();
  }
}
