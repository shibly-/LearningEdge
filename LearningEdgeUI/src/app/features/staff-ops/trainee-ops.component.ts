import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { OrganizationContextService } from '../../core/services/organization-context.service';
import { AssignmentStore } from '../../core/stores/assignment.store';
import { MessageStore } from '../../core/stores/message.store';
import { UserStore } from '../../core/stores/user.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';

@Component({
  selector: 'app-trainee-ops',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    BadgeComponent,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    ModalShellComponent,
    SpinnerComponent,
  ],
  templateUrl: './trainee-ops.component.html',
  styleUrl: './trainee-ops.component.scss',
})
export class TraineeOpsComponent {
  protected readonly users = inject(UserStore);
  protected readonly assignments = inject(AssignmentStore);
  protected readonly messages = inject(MessageStore);

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly context = inject(OrganizationContextService);
  private readonly messagingId = signal<string | null>(null);

  protected readonly messagingTraineeId = this.messagingId.asReadonly();

  protected readonly isLoading = computed(
    () => this.users.isLoading() || this.assignments.isLoading(),
  );
  protected readonly errorMessage = computed(() => this.users.error() ?? this.assignments.error());

  protected readonly messagingTraineeName = computed(() => {
    const id = this.messagingId();
    if (id === null) {
      return '';
    }
    const trainee = this.users.findById(id);
    return trainee === undefined ? '' : `${trainee.firstName} ${trainee.lastName}`;
  });

  /** One row per trainee with their assignment rollup. */
  protected readonly rows = computed(() =>
    this.users.trainees().map((trainee) => {
      const owned = this.assignments.items().filter((a) => a.traineeId === trainee.id);
      return {
        id: trainee.id,
        name: `${trainee.firstName} ${trainee.lastName}`.trim(),
        email: trainee.email,
        total: owned.length,
        completed: owned.filter((a) => a.status === 'completed').length,
        overdue: owned.filter((a) => a.status === 'overdue').length,
        latestDue: owned.reduce<string | null>(
          (latest, a) =>
            a.dueAt !== null && (latest === null || a.dueAt > latest) ? a.dueAt : latest,
          null,
        ),
      };
    }),
  );

  protected readonly form = this.fb.group({
    subject: this.fb.control('', [Validators.required]),
    body: this.fb.control('', [Validators.required]),
  });

  constructor() {
    this.reload();
  }

  protected reload(): void {
    this.users.loadForActiveOrganization();
    this.assignments.loadForActiveOrganization();
  }

  protected openMessage(traineeId: string): void {
    this.form.reset({ subject: '', body: '' });
    this.messagingId.set(traineeId);
  }

  protected closeMessage(): void {
    this.messagingId.set(null);
  }

  protected sendMessage(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const recipientId = this.messagingId();
    const organizationId = this.context.activeOrganizationId();
    if (recipientId === null || organizationId === null) {
      return;
    }

    this.messages.send({ ...this.form.getRawValue(), organizationId, recipientId }, () =>
      this.messagingId.set(null),
    );
  }
}
