import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { CreateUserCommand, UpdateUserCommand, UserDto } from '../models/user';
import { UserRole } from '../models/user-role';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS } from './repository-support';

/** Both filters are optional: no organization means all, no roles means every role. */
export interface UserListFilter {
  readonly organizationId?: string | null;
  readonly roles?: readonly UserRole[];
}

export abstract class UserRepository {
  abstract listAll(filter: UserListFilter): Observable<readonly UserDto[]>;
  abstract listByOrganization(organizationId: string): Observable<readonly UserDto[]>;
  abstract getById(id: string): Observable<UserDto | null>;
  abstract create(command: CreateUserCommand): Observable<string>;
  abstract update(id: string, command: UpdateUserCommand): Observable<UserDto>;
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

  override update(id: string, command: UpdateUserCommand): Observable<UserDto> {
    return this.api.put<UpdateUserCommand, UserDto>(apiPaths.user.byId(id), command);
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

  override create(command: CreateUserCommand): Observable<string> {
    const problem = this.validate(command, null);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const id = this.db.nextGuid();
    this.db.users = [...this.db.users, this.toDto(id, command)];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }

  override update(id: string, command: UpdateUserCommand): Observable<UserDto> {
    if (!this.db.users.some((user) => user.id === id)) {
      return throwError(() => new ApiFailure('not-found', `No user found with Id ${id}.`, 404));
    }
    const problem = this.validate(command, id);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const updated = this.toDto(id, command);
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
    };
  }
}
