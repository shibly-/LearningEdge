import { Injectable, inject } from '@angular/core';
import { Observable, delay, forkJoin, of, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { ApiFailure } from '../http/api-result';
import type { AssignTrainingCommand, TrainingAssignment, TrainingResult } from '../models/training';
import { fullName } from '../models/user';
import { MockDb } from './mock/mock-db';
import { TrainingRepository } from './training.repository';
import { UserRepository } from './user.repository';
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
  private readonly trainings = inject(TrainingRepository);
  private readonly users = inject(UserRepository);
  private readonly auth = inject(AuthService);

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

  /**
   * Trainings and trainees are read through their repositories, so this works
   * whether those come from the API or the sample data.
   */
  override assign(command: AssignTrainingCommand): Observable<number> {
    if (command.traineeIds.length === 0) {
      return throwError(() => new ApiFailure('validation', 'Select at least one trainee.'));
    }

    return forkJoin({
      trainings: this.trainings.listByOrganization(command.organizationId),
      users: this.users.listByOrganization(command.organizationId),
    }).pipe(
      switchMap(({ trainings, users }) => {
        const training = trainings.find((t) => t.id === command.trainingId);
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
          const trainee = users.find((user) => user.id === traineeId);
          created.push({
            id: this.db.nextId('asg'),
            organizationId: command.organizationId,
            trainingId: command.trainingId,
            trainingTitle: training.name,
            traineeId,
            traineeName: trainee === undefined ? 'Unknown trainee' : fullName(trainee),
            assignedById: this.auth.currentUser()?.id ?? 'unknown',
            assignedAt: new Date().toISOString(),
            dueAt: command.dueAt,
            status: 'assigned',
          });
        }

        this.db.assignments = [...this.db.assignments, ...created];
        return of(created.length).pipe(delay(MOCK_LATENCY_MS));
      }),
    );
  }
}
