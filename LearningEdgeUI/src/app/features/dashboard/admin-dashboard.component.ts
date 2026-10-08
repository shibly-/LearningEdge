import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AssignmentStore } from '../../core/stores/assignment.store';
import { CategoryStore } from '../../core/stores/category.store';
import { TrainingStore } from '../../core/stores/training.store';
import { UserStore } from '../../core/stores/user.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { StatCardComponent } from '../../shared/components/stat-card.component';

@Component({
  selector: 'app-admin-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    PageHeaderComponent,
    StatCardComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    BadgeComponent,
  ],
  template: `
    <app-page-header title="Dashboard" subtitle="Organization overview">
      <a class="le-btn le-btn-secondary" routerLink="/users">Users & staff</a>
      <a class="le-btn" routerLink="/trainings">Trainings</a>
    </app-page-header>

    @if (isLoading()) {
      <div class="le-card" style="padding: 18px">
        <app-skeleton [count]="5" label="Loading dashboard" />
      </div>
    } @else if (errorMessage() !== null) {
      <app-error-state [message]="errorMessage() ?? ''" (retry)="reload()" />
    } @else {
      <div class="le-grid-stats">
        <app-stat-card label="Staff" [value]="users.staff().length" />
        <app-stat-card label="Trainees" [value]="users.trainees().length" />
        <app-stat-card label="Categories" [value]="categories.count()" />
        <app-stat-card label="Trainings" [value]="trainings.count()" hint="Active and inactive" />
        <app-stat-card
          label="Pass rate"
          [value]="passRateLabel()"
          hint="Across completed assignments"
        />
        <app-stat-card label="Overdue" [value]="assignments.overdue().length" />
      </div>

      @if (assignments.isEmpty()) {
        <app-empty-state
          title="No assignments yet"
          message="Assign a training to your trainees to start tracking progress here."
        />
      } @else {
        <section class="le-card le-table-wrap">
          <table class="le-table">
            <caption>
              Most recent assignments
            </caption>
            <thead>
              <tr>
                <th scope="col">Trainee</th>
                <th scope="col">Training</th>
                <th scope="col">Status</th>
                <th scope="col">Due</th>
              </tr>
            </thead>
            <tbody>
              @for (assignment of recent(); track assignment.id) {
                <tr>
                  <th scope="row">{{ assignment.traineeName }}</th>
                  <td>{{ assignment.trainingTitle }}</td>
                  <td><app-badge [status]="assignment.status" /></td>
                  <td>
                    {{ assignment.dueAt === null ? '—' : (assignment.dueAt | date: 'mediumDate') }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </section>
      }
    }
  `,
})
export class AdminDashboardComponent {
  protected readonly users = inject(UserStore);
  protected readonly categories = inject(CategoryStore);
  protected readonly trainings = inject(TrainingStore);
  protected readonly assignments = inject(AssignmentStore);

  protected readonly isLoading = computed(
    () =>
      this.users.isLoading() ||
      this.categories.isLoading() ||
      this.trainings.isLoading() ||
      this.assignments.isLoading(),
  );

  protected readonly errorMessage = computed(
    () =>
      this.users.error() ??
      this.categories.error() ??
      this.trainings.error() ??
      this.assignments.error(),
  );

  protected readonly passRateLabel = computed(() => {
    const rate = this.assignments.passRate();
    return rate === null ? '—' : `${rate}%`;
  });

  protected readonly recent = computed(() =>
    [...this.assignments.items()]
      .sort((a, b) => b.assignedAt.localeCompare(a.assignedAt))
      .slice(0, 8),
  );

  constructor() {
    this.reload();
  }

  protected reload(): void {
    this.users.loadForActiveOrganization();
    this.categories.loadForActiveOrganization();
    this.trainings.loadForActiveOrganization();
    this.assignments.loadForActiveOrganization();
  }
}
