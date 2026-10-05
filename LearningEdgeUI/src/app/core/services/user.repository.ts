import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { CreateUserCommand, UserDto } from '../models/user';
import { UserRole } from '../models/user-role';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS, noListEndpoint } from './repository-support';

export abstract class UserRepository {
  /** MOCK: no backend list endpoint (spec 3.4). */
  abstract listByOrganization(organizationId: string): Observable<readonly UserDto[]>;
  abstract getById(id: string): Observable<UserDto | null>;
  abstract create(command: CreateUserCommand): Observable<string>;
}

@Injectable()
export class HttpUserRepository extends UserRepository {
  private readonly api = inject(ApiService);

  override listByOrganization(_organizationId: string): Observable<readonly UserDto[]> {
    return throwError(() => noListEndpoint('user'));
  }

  /** Rate limited to 5 requests per 10s; retryInterceptor absorbs the 503s. */
  override getById(id: string): Observable<UserDto | null> {
    return this.api.getOptional<UserDto>(apiPaths.user.byId(id));
  }

  override create(command: CreateUserCommand): Observable<string> {
    return this.api.post(apiPaths.user.create(), command);
  }
}

@Injectable()
export class MockUserRepository extends UserRepository {
  private readonly db = inject(MockDb);

  override listByOrganization(organizationId: string): Observable<readonly UserDto[]> {
    const users = this.db.users.filter((user) => user.organizationId === organizationId);
    return of(users).pipe(delay(MOCK_LATENCY_MS));
  }

  override getById(id: string): Observable<UserDto | null> {
    return of(this.db.users.find((user) => user.id === id) ?? null).pipe(delay(MOCK_LATENCY_MS));
  }

  override create(command: CreateUserCommand): Observable<string> {
    // Mirrors the server-side ArgumentNullException guards on User.
    if (command.firstName.trim().length === 0) {
      return throwError(() => new ApiFailure('envelope', 'First name is required.'));
    }
    if (command.email.trim().length === 0) {
      return throwError(() => new ApiFailure('envelope', 'Email is required.'));
    }
    if (
      this.db.users.some((user) => user.email.toLowerCase() === command.email.trim().toLowerCase())
    ) {
      return throwError(() => new ApiFailure('envelope', 'That email is already registered.'));
    }

    const id = this.db.nextGuid();
    this.db.users = [
      ...this.db.users,
      {
        id,
        firstName: command.firstName.trim(),
        lastName: command.lastName.trim(),
        email: command.email.trim(),
        // The server falls back to Learner for an undefined enum value.
        role: command.role ?? UserRole.Learner,
        organizationId: command.organizationId,
      },
    ];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }
}
