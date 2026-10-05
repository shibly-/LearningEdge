import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import type { AssignTrainingCommand, TrainingAssignment, TrainingResult } from '../models/training';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS, notImplementedUpstream } from './repository-support';

// MOCK: no backend endpoint for assignments or results (spec 3.7).
export abstract class AssignmentRepository {
  abstract listByOrganization(organizationId: string): Observable<readonly TrainingAssignment[]>;
  abstract listByTrainee(
    organizationId: string,
    traineeId: string,
  ): Observable<readonly TrainingAssignment[]>;
  abstract resultsByOrganization(organizationId: string): Observable<readonly TrainingResult[]>;
  abstract resultsByTrainee(
    organizationId: string,
    traineeId: string,
  ): Observable<readonly TrainingResult[]>;
  abstract assign(command: AssignTrainingCommand): Observable<number>;
}

@Injectable()
export class HttpAssignmentRepository extends AssignmentRepository {
  override listByOrganization(_organizationId: string): Observable<readonly TrainingAssignment[]> {
    return throwError(() => notImplementedUpstream('Listing assignments'));
  }

  override listByTrainee(
    _organizationId: string,
    _traineeId: string,
  ): Observable<readonly TrainingAssignment[]> {
    return throwError(() => notImplementedUpstream('Listing a trainee\u2019s assignments'));
  }

  override resultsByOrganization(_organizationId: string): Observable<readonly TrainingResult[]> {
    return throwError(() => notImplementedUpstream('Listing results'));
  }

  override resultsByTrainee(
    _organizationId: string,
    _traineeId: string,
  ): Observable<readonly TrainingResult[]> {
    return throwError(() => notImplementedUpstream('Listing a trainee\u2019s results'));
  }

  override assign(_command: AssignTrainingCommand): Observable<number> {
    return throwError(() => notImplementedUpstream('Assigning a training'));
  }
}

@Injectable()
export class MockAssignmentRepository extends AssignmentRepository {
  private readonly db = inject(MockDb);

  override listByOrganization(organizationId: string): Observable<readonly TrainingAssignment[]> {
    const items = this.db.assignments.filter((a) => a.organizationId === organizationId);
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override listByTrainee(
    organizationId: string,
    traineeId: string,
  ): Observable<readonly TrainingAssignment[]> {
    const items = this.db.assignments.filter(
      (a) => a.organizationId === organizationId && a.traineeId === traineeId,
    );
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override resultsByOrganization(organizationId: string): Observable<readonly TrainingResult[]> {
    const items = this.db.results.filter((r) => r.organizationId === organizationId);
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override resultsByTrainee(
    organizationId: string,
    traineeId: string,
  ): Observable<readonly TrainingResult[]> {
    const items = this.db.results.filter(
      (r) => r.organizationId === organizationId && r.traineeId === traineeId,
    );
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override assign(command: AssignTrainingCommand): Observable<number> {
    if (command.traineeIds.length === 0) {
      return throwError(() => new ApiFailure('envelope', 'Select at least one trainee.'));
    }

    const training = this.db.trainings.find((t) => t.id === command.trainingId);
    if (training === undefined) {
      return throwError(() => new ApiFailure('not-found', 'That training no longer exists.'));
    }

    const existing = new Set(
      this.db.assignments
        .filter((a) => a.trainingId === command.trainingId)
        .map((a) => a.traineeId),
    );

    const created: TrainingAssignment[] = [];
    for (const traineeId of command.traineeIds) {
      if (existing.has(traineeId)) {
        continue;
      }
      const trainee = this.db.users.find((user) => user.id === traineeId);
      created.push({
        id: this.db.nextId('asg'),
        organizationId: command.organizationId,
        trainingId: command.trainingId,
        trainingTitle: training.title,
        traineeId,
        traineeName:
          trainee === undefined ? 'Unknown trainee' : `${trainee.firstName} ${trainee.lastName}`,
        assignedById: 'current-user',
        assignedAt: new Date().toISOString(),
        dueAt: command.dueAt,
        status: 'assigned',
      });
    }

    this.db.assignments = [...this.db.assignments, ...created];
    return of(created.length).pipe(delay(MOCK_LATENCY_MS));
  }
}
