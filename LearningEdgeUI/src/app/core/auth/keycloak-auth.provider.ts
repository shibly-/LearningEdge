import { Injectable, inject } from '@angular/core';
import Keycloak from 'keycloak-js';
import { Observable, map, of, throwError } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import { fullName } from '../models/user';
import type { UserDto } from '../models/user';
import { UserRole, isPlatformScoped } from '../models/user-role';
import { UserRepository } from '../services/user.repository';
import type { AuthProvider } from './auth-provider';
import type { AuthUser } from './auth-user';
import { readTokenIdentity } from './token-claims';
import type { TokenIdentity } from './token-claims';

/**
 * Authorization code flow with PKCE against Keycloak.
 *
 * The token establishes *who* is calling; the API's user record establishes
 * what they may do. Role and organization therefore come from the record, not
 * from claims, so tenant scoping has a single source of truth.
 */
@Injectable({ providedIn: 'root' })
export class KeycloakAuthProvider implements AuthProvider {
  private readonly keycloak = inject(Keycloak);
  private readonly users = inject(UserRepository);

  restore(): Observable<AuthUser | null> {
    if (this.keycloak.authenticated !== true) {
      return of(null);
    }

    const identity = readTokenIdentity(this.keycloak.tokenParsed);
    if (identity === null) {
      return throwError(
        () => new ApiFailure('malformed', 'The access token is missing its subject claim.'),
      );
    }

    return this.users.me(identity.email).pipe(map((record) => toAuthUser(identity, record)));
  }

  login(returnUrl: string | null): Promise<void> {
    return this.keycloak.login({ redirectUri: appUrl(returnUrl) });
  }

  logout(): Promise<void> {
    return this.keycloak.logout({ redirectUri: appUrl('/login') });
  }

  token(): string | null {
    return this.keycloak.token ?? null;
  }
}

function toAuthUser(identity: TokenIdentity, record: UserDto | null): AuthUser {
  if (record !== null) {
    return {
      id: record.id,
      username: identity.username,
      displayName: fullName(record) || identity.displayName,
      email: record.email || identity.email,
      role: record.role,
      organizationId: isPlatformScoped(record.role) ? null : record.organizationId,
      isProvisioned: true,
    };
  }

  // A platform operator can work without a user record: creating the first
  // organization and its admin is how a fresh database is seeded.
  if (identity.role !== UserRole.SysAdmin) {
    throw new ApiFailure(
      'not-found',
      `No LearningEdge user has the email ${identity.email || identity.username}. Ask a system admin to create this user first.`,
    );
  }

  return {
    id: identity.subject,
    username: identity.username,
    displayName: identity.displayName,
    email: identity.email,
    role: UserRole.SysAdmin,
    organizationId: null,
    isProvisioned: false,
  };
}

/** Keycloak rejects a redirect the client does not list, but validate anyway. */
function appUrl(path: string | null): string {
  const safe = path !== null && path.startsWith('/') && !path.startsWith('//') ? path : '/';
  return `${window.location.origin}${safe}`;
}
