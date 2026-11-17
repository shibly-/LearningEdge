import { Injectable, inject } from '@angular/core';
import { Observable, delay, map, of } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { PlatformSnapshot } from '../models/platform-snapshot';
import { UserActivationStatus } from '../models/user';
import { UserRole } from '../models/user-role';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS } from './repository-support';

export abstract class PlatformRepository {
  abstract snapshot(): Observable<PlatformSnapshot>;
}

@Injectable()
export class HttpPlatformRepository extends PlatformRepository {
  private readonly api = inject(ApiService);

  override snapshot(): Observable<PlatformSnapshot> {
    return this.api
      .get<unknown>(apiPaths.platform.snapshot())
      .pipe(map((body) => readSnapshot(body)));
  }
}

@Injectable()
export class MockPlatformRepository extends PlatformRepository {
  private readonly db = inject(MockDb);

  override snapshot(): Observable<PlatformSnapshot> {
    const learners = this.db.users.filter((user) => user.role === UserRole.Learner);
    const snapshot: PlatformSnapshot = {
      organizationsActive: this.db.organizations.filter((org) => org.isActive).length,
      organizationsInactive: this.db.organizations.filter((org) => !org.isActive).length,
      learnersActive: learners.filter((user) => user.activationStatus === UserActivationStatus.Active)
        .length,
      learnersPending: learners.filter((user) => user.activationStatus === UserActivationStatus.Pending)
        .length,
      learnersInactive: learners.filter(
        (user) => user.activationStatus === UserActivationStatus.Inactive,
      ).length,
      trainingsActive: this.db.trainings.filter((training) => training.isActive).length,
      trainingsInactive: this.db.trainings.filter((training) => !training.isActive).length,
    };
    return of(snapshot).pipe(delay(MOCK_LATENCY_MS));
  }
}

function readSnapshot(body: unknown): PlatformSnapshot {
  if (typeof body !== 'object' || body === null) {
    throw new ApiFailure('malformed', 'The platform snapshot was not a record.');
  }

  const record = body as Record<string, unknown>;
  return {
    organizationsActive: readCount(record, 'organizationsActive'),
    organizationsInactive: readCount(record, 'organizationsInactive'),
    learnersActive: readCount(record, 'learnersActive'),
    learnersPending: readCount(record, 'learnersPending'),
    learnersInactive: readCount(record, 'learnersInactive'),
    trainingsActive: readCount(record, 'trainingsActive'),
    trainingsInactive: readCount(record, 'trainingsInactive'),
  };
}

function readCount(record: Record<string, unknown>, name: string): number {
  const value = record[name];
  if (typeof value !== 'number' || !Number.isInteger(value) || value < 0) {
    throw new ApiFailure('malformed', `The snapshot field ${name} is not a count.`);
  }
  return value;
}
