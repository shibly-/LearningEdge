import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map, of, switchMap, throwError } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import type { UserDto } from '../models/user';
import { fullName } from '../models/user';
import { isPlatformScoped } from '../models/user-role';
import { OrganizationRepository } from '../services/organization.repository';
import { UserRepository } from '../services/user.repository';
import type { AuthUser } from './auth-user';

/**
 * Mock login against the live API: the password is checked locally, then the
 * identity is swapped for the API's user record with the same email, so every
 * request carries real ids (uploader id, organization scope).
 *
 * The API has no lookup-by-email endpoint, so this scans each organization's
 * users. Fine for dev data; real authentication will replace it.
 */
@Injectable({ providedIn: 'root' })
export class ApiIdentityResolver {
  private readonly organizations = inject(OrganizationRepository);
  private readonly users = inject(UserRepository);

  resolve(user: AuthUser): Observable<AuthUser> {
    const email = user.email.trim().toLowerCase();
    const platform = isPlatformScoped(user.role);

    return this.allUsers().pipe(
      switchMap((users) => {
        const matches = users.filter((u) => u.email.trim().toLowerCase() === email);

        if (matches.length === 1) {
          return of(toAuthUser(user, matches[0]));
        }
        if (matches.length > 1) {
          return throwError(
            () =>
              new ApiFailure(
                'conflict',
                `${user.email} belongs to ${matches.length} organizations in the API. Give this login a unique email.`,
              ),
          );
        }
        // A platform operator can work without a user record: creating the
        // first organization and its admin is how a fresh database is seeded.
        if (platform) {
          return of(user);
        }
        return throwError(
          () =>
            new ApiFailure(
              'not-found',
              `No API user has the email ${user.email}. Sign in as a system admin and create this user first.`,
            ),
        );
      }),
    );
  }

  private allUsers(): Observable<readonly UserDto[]> {
    return this.organizations.list().pipe(
      switchMap((orgs) =>
        orgs.length === 0
          ? of([] as (readonly UserDto[])[])
          : forkJoin(orgs.map((org) => this.users.listByOrganization(org.id))),
      ),
      map((groups) => groups.flat()),
    );
  }
}

function toAuthUser(login: AuthUser, record: UserDto): AuthUser {
  return {
    ...login,
    id: record.id,
    // The API record is the source of truth for role and tenant.
    role: record.role,
    organizationId: isPlatformScoped(record.role) ? null : record.organizationId,
    displayName: fullName(record) || login.displayName,
    email: record.email,
  };
}
