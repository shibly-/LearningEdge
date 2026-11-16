import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { CreateUserCommand, RegisterLearnerCommand, UpdateUserCommand, UserDto } from '../models/user';
import { UserActivationStatus } from '../models/user';
import { UserRole } from '../models/user-role';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS } from './repository-support';

/** Both filters are optional: no organization means all, no roles means every role. */
export interface UserListFilter {
  readonly organizationId?: string | null;
  readonly roles?: readonly UserRole[];
  readonly activationStatus?: UserActivationStatus;
}

export abstract class UserRepository {
  abstract listAll(filter: UserListFilter): Observable<readonly UserDto[]>;
  abstract listByOrganization(organizationId: string): Observable<readonly UserDto[]>;
  abstract getById(id: string): Observable<UserDto | null>;
  abstract create(command: CreateUserCommand): Observable<string>;
  abstract register(command: RegisterLearnerCommand): Observable<string>;
  abstract approve(id: string): Observable<UserDto>;
  abstract update(id: string, command: UpdateUserCommand): Observable<UserDto>;
  /**
   * The record for the signed-in principal, or null when none exists yet.
   *
   * The live route derives the principal from the bearer token and ignores
   * `email`; the sample-data route has no token to read, so it matches on the
   * email claim instead.
   */
  abstract me(email: string): Observable<UserDto | null>;
}

@Injectable()
export class HttpUserRepository extends UserRepository {
  private readonly api = inject(ApiService);

  override listAll(filter: UserListFilter): Observable<readonly UserDto[]> {
    return this.api.get<readonly UserDto[]>(apiPaths.user.list(filter));
  }

  override listByOrganization(organizationId: string): Observable<readonly UserDto[]> {
    return this.api.get<readonly UserDto[]>(apiPaths.organization.users(organizationId));
  }

  /** Rate limited to 5 requests per 10s; retryInterceptor absorbs the 503s. */
  override getById(id: string): Observable<UserDto | null> {
    return this.api.getOptional<UserDto>(apiPaths.user.byId(id));
  }

  override create(command: CreateUserCommand): Observable<string> {
    return this.api.post(apiPaths.user.create(), command);
  }

  override register(command: RegisterLearnerCommand): Observable<string> {
    return this.api.post(apiPaths.user.register(), command);
  }

  override approve(id: string): Observable<UserDto> {
    return this.api.postResult<Record<string, never>, UserDto>(apiPaths.user.approve(id), {});
  }

  override update(id: string, command: UpdateUserCommand): Observable<UserDto> {
    return this.api.put<UpdateUserCommand, UserDto>(apiPaths.user.byId(id), command);
  }

  override me(_email: string): Observable<UserDto | null> {
    return this.api.getOptional<UserDto>(apiPaths.user.me());
  }
}

@Injectable()
export class MockUserRepository extends UserRepository {
  private readonly db = inject(MockDb);

  override listAll(filter: UserListFilter): Observable<readonly UserDto[]> {
    const { organizationId, roles = [] } = filter;
    if (organizationId && !this.db.organizations.some((org) => org.id === organizationId)) {
      return throwError(
        () => new ApiFailure('not-found', `No organization found with Id ${organizationId}.`, 404),
      );
    }

    const users = this.db.users
      .filter((user) => !organizationId || user.organizationId === organizationId)
      .filter((user) => roles.length === 0 || roles.includes(user.role))
      .filter(
        (user) =>
          filter.activationStatus == null || user.activationStatus === filter.activationStatus,
      )
      .sort(
        (a, b) => a.lastName.localeCompare(b.lastName) || a.firstName.localeCompare(b.firstName),
      );
    return of(users).pipe(delay(MOCK_LATENCY_MS));
  }

  override listByOrganization(organizationId: string): Observable<readonly UserDto[]> {
    const users = this.db.users.filter((user) => user.organizationId === organizationId);
    return of(users).pipe(delay(MOCK_LATENCY_MS));
  }

  override getById(id: string): Observable<UserDto | null> {
    return of(this.db.users.find((user) => user.id === id) ?? null).pipe(delay(MOCK_LATENCY_MS));
  }

  override me(email: string): Observable<UserDto | null> {
    const wanted = email.trim().toLowerCase();
    const match = this.db.users.find((user) => user.email.trim().toLowerCase() === wanted) ?? null;
    return of(match).pipe(delay(MOCK_LATENCY_MS));
  }

  override create(command: CreateUserCommand): Observable<string> {
    const problem = this.validate(command, null);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const id = this.db.nextGuid();
    this.db.users = [...this.db.users, this.toDto(id, command)];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }

  // MOCK: no Keycloak. The live approve route creates the realm user.
  override register(command: RegisterLearnerCommand): Observable<string> {
    const problem = this.validate(
      {
        firstName: command.firstName,
        lastName: command.lastName,
        email: command.email,
        role: UserRole.Learner,
        organizationId: command.organizationId,
      },
      null,
    );
    if (problem !== null) {
      return throwError(() => problem);
    }
    if (command.password.length < 8) {
      return throwError(() => new ApiFailure('validation', 'Password must be at least 8 characters.', 400));
    }

    const id = this.db.nextGuid();
    this.db.users = [
      ...this.db.users,
      {
        ...this.toDto(id, {
          firstName: command.firstName,
          lastName: command.lastName,
          email: command.email,
          role: UserRole.Learner,
          organizationId: command.organizationId,
        }),
        activationStatus: UserActivationStatus.Pending,
      },
    ];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }

  // MOCK: no Keycloak. Marks the user active without calling the realm.
  override approve(id: string): Observable<UserDto> {
    const existing = this.db.users.find((user) => user.id === id);
    if (existing === undefined) {
      return throwError(() => new ApiFailure('not-found', `No user found with Id ${id}.`, 404));
    }
    if (existing.activationStatus !== UserActivationStatus.Pending) {
      return throwError(() => new ApiFailure('conflict', 'This user is already approved.', 409));
    }
    const approved: UserDto = { ...existing, activationStatus: UserActivationStatus.Active };
    this.db.users = this.db.users.map((user) => (user.id === id ? approved : user));
    return of(approved).pipe(delay(MOCK_LATENCY_MS));
  }

  override update(id: string, command: UpdateUserCommand): Observable<UserDto> {
    const current = this.db.users.find((user) => user.id === id);
    if (current === undefined) {
      return throwError(() => new ApiFailure('not-found', `No user found with Id ${id}.`, 404));
    }
    const problem = this.validate(command, id);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const updated = { ...this.toDto(id, command), activationStatus: current.activationStatus };
    this.db.users = this.db.users.map((user) => (user.id === id ? updated : user));
    return of(updated).pipe(delay(MOCK_LATENCY_MS));
  }

  private validate(command: CreateUserCommand, ownId: string | null): ApiFailure | null {
    if (command.firstName.trim().length === 0) {
      return new ApiFailure('validation', 'First name is required.', 400);
    }
    const email = command.email.trim().toLowerCase();
    if (email.length === 0) {
      return new ApiFailure('validation', 'Email is required.', 400);
    }
    // Emails are unique per organization, as on the server.
    const taken = this.db.users.some(
      (user) =>
        user.id !== ownId &&
        user.organizationId === command.organizationId &&
        user.email.toLowerCase() === email,
    );
    return taken
      ? new ApiFailure(
          'conflict',
          `User with email ${command.email} already exists in this organization.`,
          409,
        )
      : null;
  }

  private toDto(id: string, command: CreateUserCommand): UserDto {
    return {
      id,
      firstName: command.firstName.trim(),
      lastName: command.lastName.trim(),
      email: command.email.trim(),
      role: command.role ?? UserRole.Learner,
      organizationId: command.organizationId,
      activationStatus: UserActivationStatus.Active,
    };
  }
}
