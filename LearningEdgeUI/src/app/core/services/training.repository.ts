import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import type { CreateTrainingCommand, Training } from '../models/training';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS, notImplementedUpstream } from './repository-support';

// MOCK: no backend endpoint for trainings (spec 3.7).
export abstract class TrainingRepository {
  abstract listByOrganization(organizationId: string): Observable<readonly Training[]>;
  abstract listByCategory(
    organizationId: string,
    categoryId: string,
  ): Observable<readonly Training[]>;
  abstract getById(id: string): Observable<Training | null>;
  abstract create(command: CreateTrainingCommand): Observable<string>;
}

@Injectable()
export class HttpTrainingRepository extends TrainingRepository {
  override listByOrganization(_organizationId: string): Observable<readonly Training[]> {
    return throwError(() => notImplementedUpstream('Listing trainings'));
  }

  override listByCategory(
    _organizationId: string,
    _categoryId: string,
  ): Observable<readonly Training[]> {
    return throwError(() => notImplementedUpstream('Listing trainings by category'));
  }

  override getById(_id: string): Observable<Training | null> {
    return throwError(() => notImplementedUpstream('Loading a training'));
  }

  override create(_command: CreateTrainingCommand): Observable<string> {
    return throwError(() => notImplementedUpstream('Creating a training'));
  }
}

@Injectable()
export class MockTrainingRepository extends TrainingRepository {
  private readonly db = inject(MockDb);

  override listByOrganization(organizationId: string): Observable<readonly Training[]> {
    const items = this.db.trainings.filter((t) => t.organizationId === organizationId);
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override listByCategory(
    organizationId: string,
    categoryId: string,
  ): Observable<readonly Training[]> {
    const items = this.db.trainings.filter(
      (t) => t.organizationId === organizationId && t.categoryId === categoryId,
    );
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override getById(id: string): Observable<Training | null> {
    return of(this.db.trainings.find((t) => t.id === id) ?? null).pipe(delay(MOCK_LATENCY_MS));
  }

  override create(command: CreateTrainingCommand): Observable<string> {
    const title = command.title.trim();
    if (title.length === 0) {
      return throwError(() => new ApiFailure('envelope', 'Training title is required.'));
    }
    if (command.durationMinutes <= 0) {
      return throwError(() => new ApiFailure('envelope', 'Duration must be greater than zero.'));
    }

    const id = this.db.nextId('trn');
    this.db.trainings = [
      ...this.db.trainings,
      {
        id,
        organizationId: command.organizationId,
        categoryId: command.categoryId,
        title,
        description: command.description.trim(),
        status: 'draft',
        durationMinutes: command.durationMinutes,
        passMark: command.passMark,
        createdAt: new Date().toISOString(),
      },
    ];
    this.db.categories = this.db.categories.map((c) =>
      c.id === command.categoryId ? { ...c, trainingCount: c.trainingCount + 1 } : c,
    );
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }
}
