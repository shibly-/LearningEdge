import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import type { Category, CreateCategoryCommand } from '../models/category';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS, notImplementedUpstream } from './repository-support';

// MOCK: no backend endpoint for categories at all (spec 3.7).
export abstract class CategoryRepository {
  abstract listByOrganization(organizationId: string): Observable<readonly Category[]>;
  abstract getById(id: string): Observable<Category | null>;
  abstract create(command: CreateCategoryCommand): Observable<string>;
}

@Injectable()
export class HttpCategoryRepository extends CategoryRepository {
  override listByOrganization(_organizationId: string): Observable<readonly Category[]> {
    return throwError(() => notImplementedUpstream('Listing categories'));
  }

  override getById(_id: string): Observable<Category | null> {
    return throwError(() => notImplementedUpstream('Loading a category'));
  }

  override create(_command: CreateCategoryCommand): Observable<string> {
    return throwError(() => notImplementedUpstream('Creating a category'));
  }
}

@Injectable()
export class MockCategoryRepository extends CategoryRepository {
  private readonly db = inject(MockDb);

  override listByOrganization(organizationId: string): Observable<readonly Category[]> {
    const items = this.db.categories.filter((c) => c.organizationId === organizationId);
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override getById(id: string): Observable<Category | null> {
    return of(this.db.categories.find((c) => c.id === id) ?? null).pipe(delay(MOCK_LATENCY_MS));
  }

  override create(command: CreateCategoryCommand): Observable<string> {
    const name = command.name.trim();
    if (name.length === 0) {
      return throwError(() => new ApiFailure('envelope', 'Category name is required.'));
    }
    const duplicate = this.db.categories.some(
      (c) =>
        c.organizationId === command.organizationId && c.name.toLowerCase() === name.toLowerCase(),
    );
    if (duplicate) {
      return throwError(() => new ApiFailure('envelope', 'That category already exists.'));
    }

    const id = this.db.nextId('cat');
    this.db.categories = [
      ...this.db.categories,
      {
        id,
        organizationId: command.organizationId,
        name,
        description: command.description.trim(),
        trainingCount: 0,
        createdAt: new Date().toISOString(),
      },
    ];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }
}
