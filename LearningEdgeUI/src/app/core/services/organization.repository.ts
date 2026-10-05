import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { CreateOrganizationCommand, OrganizationDto } from '../models/organization';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS, noListEndpoint } from './repository-support';

export abstract class OrganizationRepository {
  /** MOCK: no backend list endpoint (spec 3.4). */
  abstract list(): Observable<readonly OrganizationDto[]>;
  abstract getById(id: string): Observable<OrganizationDto | null>;
  abstract create(command: CreateOrganizationCommand): Observable<string>;
}

/** Live wiring for the two operations the API actually implements. */
@Injectable()
export class HttpOrganizationRepository extends OrganizationRepository {
  private readonly api = inject(ApiService);

  override list(): Observable<readonly OrganizationDto[]> {
    return throwError(() => noListEndpoint('organization'));
  }

  override getById(id: string): Observable<OrganizationDto | null> {
    return this.api.getOptional<OrganizationDto>(apiPaths.organization.byId(id));
  }

  override create(command: CreateOrganizationCommand): Observable<string> {
    return this.api.post(apiPaths.organization.create(), command);
  }
}

@Injectable()
export class MockOrganizationRepository extends OrganizationRepository {
  private readonly db = inject(MockDb);

  override list(): Observable<readonly OrganizationDto[]> {
    return of([...this.db.organizations]).pipe(delay(MOCK_LATENCY_MS));
  }

  override getById(id: string): Observable<OrganizationDto | null> {
    const found = this.db.organizations.find((org) => org.id === id) ?? null;
    return of(found).pipe(delay(MOCK_LATENCY_MS));
  }

  override create(command: CreateOrganizationCommand): Observable<string> {
    const name = command.name.trim();
    if (name.length === 0) {
      return throwError(() => new ApiFailure('envelope', 'Organization name is required.'));
    }
    if (this.db.organizations.some((org) => org.name.toLowerCase() === name.toLowerCase())) {
      return throwError(
        () => new ApiFailure('envelope', 'An organization with that name already exists.'),
      );
    }

    const id = this.db.nextGuid();
    this.db.organizations = [
      ...this.db.organizations,
      { id, name, description: command.description.trim() },
    ];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }
}
