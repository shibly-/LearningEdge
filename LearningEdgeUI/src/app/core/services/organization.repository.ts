import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type {
  CreateOrganizationCommand,
  OrganizationDto,
  UpdateOrganizationCommand,
} from '../models/organization';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS } from './repository-support';

export abstract class OrganizationRepository {
  abstract list(): Observable<readonly OrganizationDto[]>;
  abstract getById(id: string): Observable<OrganizationDto | null>;
  abstract create(command: CreateOrganizationCommand): Observable<string>;
  abstract update(id: string, command: UpdateOrganizationCommand): Observable<OrganizationDto>;
}

@Injectable()
export class HttpOrganizationRepository extends OrganizationRepository {
  private readonly api = inject(ApiService);

  override list(): Observable<readonly OrganizationDto[]> {
    return this.api.get<readonly OrganizationDto[]>(apiPaths.organization.list());
  }

  override getById(id: string): Observable<OrganizationDto | null> {
    return this.api.getOptional<OrganizationDto>(apiPaths.organization.byId(id));
  }

  override create(command: CreateOrganizationCommand): Observable<string> {
    return this.api.post(apiPaths.organization.create(), command);
  }

  override update(id: string, command: UpdateOrganizationCommand): Observable<OrganizationDto> {
    return this.api.put<UpdateOrganizationCommand, OrganizationDto>(
      apiPaths.organization.byId(id),
      command,
    );
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
    const problem = this.validate(command, null);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const id = this.db.nextGuid();
    this.db.organizations = [
      ...this.db.organizations,
      { id, name: command.name.trim(), description: command.description.trim() },
    ];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }

  override update(id: string, command: UpdateOrganizationCommand): Observable<OrganizationDto> {
    if (!this.db.organizations.some((org) => org.id === id)) {
      return throwError(
        () => new ApiFailure('not-found', `No organization found with Id ${id}.`, 404),
      );
    }
    const problem = this.validate(command, id);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const updated: OrganizationDto = {
      id,
      name: command.name.trim(),
      description: command.description.trim(),
    };
    this.db.organizations = this.db.organizations.map((org) => (org.id === id ? updated : org));
    return of(updated).pipe(delay(MOCK_LATENCY_MS));
  }

  private validate(command: CreateOrganizationCommand, ownId: string | null): ApiFailure | null {
    const name = command.name.trim();
    if (name.length === 0) {
      return new ApiFailure('validation', 'Organization name is required.', 400);
    }
    const taken = this.db.organizations.some(
      (org) => org.id !== ownId && org.name.toLowerCase() === name.toLowerCase(),
    );
    return taken
      ? new ApiFailure('conflict', `Organization with name ${name} already exists.`, 409)
      : null;
  }
}
