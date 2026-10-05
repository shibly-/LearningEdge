import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { AssignTrainingCommand, TrainingAssignment, TrainingResult } from '../models/training';
import { AssignmentRepository } from '../services/assignment.repository';
import { OrganizationContextService } from '../services/organization-context.service';
import { ToastService } from '../services/toast.service';
import { AsyncCollectionStore, describeError } from './async-collection.store';
import { TenantResetBus } from '../services/tenant-reset-bus';

@Injectable({ providedIn: 'root' })
export class AssignmentStore extends AsyncCollectionStore<TrainingAssignment> {
  private readonly repository = inject(AssignmentRepository);
  private readonly context = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);

  private readonly resultsState = signal<readonly TrainingResult[]>([]);
  private readonly savingState = signal(false);

  readonly results = this.resultsState.asReadonly();
  readonly isSaving = this.savingState.asReadonly();

  readonly completed = computed(() => this.items().filter((a) => a.status === 'completed'));
  readonly overdue = computed(() => this.items().filter((a) => a.status === 'overdue'));
  readonly inProgress = computed(() => this.items().filter((a) => a.status === 'in-progress'));

  readonly averageScore = computed(() => {
    const scores = this.resultsState();
    if (scores.length === 0) {
      return null;
    }
    const total = scores.reduce((sum, result) => sum + result.score, 0);
    return Math.round(total / scores.length);
  });

  readonly passRate = computed(() => {
    const scores = this.resultsState();
    if (scores.length === 0) {
      return null;
    }
    const passed = scores.filter((result) => result.passed).length;
    return Math.round((passed / scores.length) * 100);
  });

  constructor() {
    super();
    // The base class only clears the assignment collection.
    inject(TenantResetBus).register(() => this.resultsState.set([]));
  }

  loadForActiveOrganization(): void {
    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      this.reset();
      return;
    }
    this.load(this.repository.listByOrganization(organizationId));
    this.loadResults(organizationId, null);
  }

  loadForTrainee(traineeId: string): void {
    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      this.reset();
      return;
    }
    this.load(this.repository.listByTrainee(organizationId, traineeId));
    this.loadResults(organizationId, traineeId);
  }

  assign(command: AssignTrainingCommand, onAssigned: () => void): void {
    if (this.savingState()) {
      return;
    }
    this.savingState.set(true);

    this.repository
      .assign(command)
      .pipe(
        finalize(() => this.savingState.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (created) => {
          if (created === 0) {
            this.toast.info('Every selected trainee already had this training.');
          } else {
            this.toast.success(
              created === 1
                ? 'Training assigned to 1 trainee.'
                : `Training assigned to ${created} trainees.`,
            );
          }
          this.loadForActiveOrganization();
          onAssigned();
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  private loadResults(organizationId: string, traineeId: string | null): void {
    const source =
      traineeId === null
        ? this.repository.resultsByOrganization(organizationId)
        : this.repository.resultsByTrainee(organizationId, traineeId);

    source.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (results) => this.resultsState.set(results),
      error: () => this.resultsState.set([]),
    });
  }
}
