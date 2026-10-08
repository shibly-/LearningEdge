import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrganizationContextService } from '../../core/services/organization-context.service';
import { AssignmentStore } from '../../core/stores/assignment.store';
import { TrainingStore } from '../../core/stores/training.store';
import { UserStore } from '../../core/stores/user.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';

@Component({
  selector: 'app-training-assign',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SpinnerComponent,
  ],
  template: `
    <app-page-header title="Assign training" subtitle="Give trainees access to an active course." />

    @if (isLoading()) {
      <div class="le-card" style="padding: 18px">
        <app-skeleton [count]="5" label="Loading assignment options" />
      </div>
    } @else if (errorMessage() !== null) {
      <app-error-state [message]="errorMessage() ?? ''" (retry)="reload()" />
    } @else if (trainings.active().length === 0) {
      <app-empty-state
        title="No active trainings"
        message="Only active trainings can be assigned. Mark a training active first."
      />
    } @else if (users.trainees().length === 0) {
      <app-empty-state
        title="No trainees"
        message="Add trainees to your organization before assigning training."
      />
    } @else {
      <form class="le-card form" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div
          class="le-field"
          [class.is-invalid]="form.controls.trainingId.touched && form.controls.trainingId.invalid"
        >
          <label for="assign-training">Training</label>
          <select id="assign-training" formControlName="trainingId">
            <option value="">Select a training…</option>
            @for (training of trainings.active(); track training.id) {
              <option [value]="training.id">{{ training.name }}</option>
            }
          </select>
          @if (form.controls.trainingId.touched && form.controls.trainingId.hasError('required')) {
            <span class="le-error">Select a training.</span>
          }
        </div>

        <fieldset
          class="le-field trainees"
          [class.is-invalid]="submitted() && selected().length === 0"
        >
          <legend>Trainees</legend>
          @for (trainee of users.trainees(); track trainee.id) {
            <label class="check">
              <input
                type="checkbox"
                [value]="trainee.id"
                [checked]="selected().includes(trainee.id)"
                (change)="toggle(trainee.id, $event)"
              />
              <span>{{ trainee.firstName }} {{ trainee.lastName }}</span>
              <span class="email">{{ trainee.email }}</span>
            </label>
          }
          @if (submitted() && selected().length === 0) {
            <span class="le-error">Select at least one trainee.</span>
          }
        </fieldset>

        <div class="le-field">
          <label for="assign-due">Due date</label>
          <input id="assign-due" type="date" formControlName="dueAt" />
          <span class="le-hint">Optional. Leave blank for no deadline.</span>
        </div>

        <button type="submit" class="le-btn" [disabled]="assignments.isSaving()">
          @if (assignments.isSaving()) {
            <app-spinner [size]="14" label="Assigning" />
            <span>Assigning…</span>
          } @else {
            <span>Assign to {{ selected().length }} trainee(s)</span>
          }
        </button>
      </form>
    }
  `,
  styles: `
    .form {
      max-width: 640px;
      padding: 20px;
    }

    .trainees {
      border: 1px solid var(--le-border);
      border-radius: var(--le-radius);
      padding: 14px;
    }

    legend {
      padding: 0 6px;
      font-weight: 600;
      font-size: 0.9rem;
    }

    .check {
      display: grid;
      grid-template-columns: auto 1fr auto;
      gap: 10px;
      align-items: center;
      padding: 6px 0;
      font-weight: 400;
    }

    .email {
      color: var(--le-text-muted);
      font-size: 0.85rem;
    }
  `,
})
export class TrainingAssignComponent {
  protected readonly trainings = inject(TrainingStore);
  protected readonly users = inject(UserStore);
  protected readonly assignments = inject(AssignmentStore);

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly context = inject(OrganizationContextService);

  private readonly selectedIds = signal<readonly string[]>([]);
  private readonly wasSubmitted = signal(false);

  protected readonly selected = this.selectedIds.asReadonly();
  protected readonly submitted = this.wasSubmitted.asReadonly();

  protected readonly isLoading = computed(
    () => this.trainings.isLoading() || this.users.isLoading(),
  );
  protected readonly errorMessage = computed(() => this.trainings.error() ?? this.users.error());

  protected readonly form = this.fb.group({
    trainingId: this.fb.control('', [Validators.required]),
    dueAt: this.fb.control(''),
  });

  constructor() {
    this.reload();
  }

  protected reload(): void {
    this.trainings.loadForActiveOrganization();
    this.users.loadForActiveOrganization();
  }

  protected toggle(traineeId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedIds.update((current) =>
      checked ? [...current, traineeId] : current.filter((id) => id !== traineeId),
    );
  }

  protected submit(): void {
    this.wasSubmitted.set(true);

    if (this.form.invalid || this.selectedIds().length === 0) {
      this.form.markAllAsTouched();
      return;
    }

    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      return;
    }

    const value = this.form.getRawValue();
    this.assignments.assign(
      {
        organizationId,
        trainingId: value.trainingId,
        traineeIds: this.selectedIds(),
        dueAt: value.dueAt === '' ? null : endOfLocalDay(value.dueAt),
      },
      () => {
        this.selectedIds.set([]);
        this.wasSubmitted.set(false);
        this.form.reset({ trainingId: '', dueAt: '' });
      },
    );
  }
}

/**
 * A date input yields `YYYY-MM-DD`, which `new Date()` reads as UTC midnight —
 * the previous day west of Greenwich. Parse it as the end of the local day instead.
 */
export function endOfLocalDay(isoDate: string): string {
  return new Date(`${isoDate}T23:59:59.999`).toISOString();
}
