import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { Category, CreateCategoryCommand, UpdateCategoryCommand } from '../models/category';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS } from './repository-support';

export abstract class CategoryRepository {
  abstract listByOrganization(organizationId: string): Observable<readonly Category[]>;
  abstract getById(organizationId: string, id: string): Observable<Category | null>;
  abstract create(command: CreateCategoryCommand): Observable<string>;
  abstract update(command: UpdateCategoryCommand): Observable<Category>;
}

@Injectable()
export class HttpCategoryRepository extends CategoryRepository {
  private readonly api = inject(ApiService);

  override listByOrganization(organizationId: string): Observable<readonly Category[]> {
    return this.api.get<readonly Category[]>(apiPaths.category.list(organizationId));
  }

  override getById(organizationId: string, id: string): Observable<Category | null> {
    return this.api.getOptional<Category>(apiPaths.category.byId(organizationId, id));
  }

  override create(command: CreateCategoryCommand): Observable<string> {
    const { organizationId, ...body } = command;
    return this.api.post(apiPaths.category.list(organizationId), body);
  }

  override update(command: UpdateCategoryCommand): Observable<Category> {
    const { organizationId, id, ...body } = command;
    return this.api.put<typeof body, Category>(apiPaths.category.byId(organizationId, id), body);
  }
}

@Injectable()
export class MockCategoryRepository extends CategoryRepository {
  private readonly db = inject(MockDb);

  override listByOrganization(organizationId: string): Observable<readonly Category[]> {
    const items = this.db.categories.filter((c) => c.organizationId === organizationId);
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override getById(organizationId: string, id: string): Observable<Category | null> {
    const found =
      this.db.categories.find((c) => c.id === id && c.organizationId === organizationId) ?? null;
    return of(found).pipe(delay(MOCK_LATENCY_MS));
  }

  override create(command: CreateCategoryCommand): Observable<string> {
    const problem = this.validate(command, null);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const id = this.db.nextId('cat');
    this.db.categories = [...this.db.categories, this.toCategory(id, command)];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }

  override update(command: UpdateCategoryCommand): Observable<Category> {
    const exists = this.db.categories.some(
      (c) => c.id === command.id && c.organizationId === command.organizationId,
    );
    if (!exists) {
      return throwError(
        () => new ApiFailure('not-found', `No category found with Id ${command.id}.`, 404),
      );
    }
    const problem = this.validate(command, command.id);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const updated = this.toCategory(command.id, command);
    this.db.categories = this.db.categories.map((c) => (c.id === command.id ? updated : c));
    return of(updated).pipe(delay(MOCK_LATENCY_MS));
  }

  private validate(command: CreateCategoryCommand, ownId: string | null): ApiFailure | null {
    const name = command.name.trim();
    if (name.length === 0) {
      return new ApiFailure('validation', 'Category name is required.', 400);
    }
    const taken = this.db.categories.some(
      (c) =>
        c.id !== ownId &&
        c.organizationId === command.organizationId &&
        c.name.toLowerCase() === name.toLowerCase(),
    );
    return taken
      ? new ApiFailure(
          'conflict',
          `A category named ${name} already exists in this organization.`,
          409,
        )
      : null;
  }

  private toCategory(id: string, command: CreateCategoryCommand): Category {
    return {
      id,
      organizationId: command.organizationId,
      name: command.name.trim(),
      description: command.description.trim(),
      isActive: command.isActive,
    };
  }
}
