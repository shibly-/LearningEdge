import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AssignmentStore } from '../../core/stores/assignment.store';
import { TrainingStore } from '../../core/stores/training.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { StatCardComponent } from '../../shared/components/stat-card.component';

@Component({
  selector: 'app-staff-dashboard',
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
    <app-page-header title="Dashboard" subtitle="Staff view — trainings you deliver">
      <a class="le-btn le-btn-secondary" routerLink="/processing">Upload material</a>
      <a class="le-btn" routerLink="/staff-ops">Trainee operations</a>
    </app-page-header>

    @if (isLoading()) {
      <div class="le-card" style="padding: 18px">
        <app-skeleton [count]="5" label="Loading dashboard" />
      </div>
    } @else if (errorMessage() !== null) {
      <app-error-state [message]="errorMessage() ?? ''" (retry)="reload()" />
    } @else {
      <div class="le-grid-stats">
        <app-stat-card label="Published trainings" [value]="trainings.published().length" />
        <app-stat-card label="Drafts" [value]="trainings.drafts().length" />
        <app-stat-card label="In progress" [value]="assignments.inProgress().length" />
        <app-stat-card label="Overdue" [value]="assignments.overdue().length" />
        <app-stat-card label="Average score" [value]="averageLabel()" />
      </div>

      @if (needsAttention().length === 0) {
        <app-empty-state
          title="Nothing needs attention"
          message="No assignment is overdue or stalled. New uploads appear under material processing."
        />
      } @else {
        <section class="le-card le-table-wrap">
          <table class="le-table">
            <caption>
              Assignments needing attention
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
              @for (assignment of needsAttention(); track assignment.id) {
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
export class StaffDashboardComponent {
  protected readonly trainings = inject(TrainingStore);
  protected readonly assignments = inject(AssignmentStore);

  protected readonly isLoading = computed(
    () => this.trainings.isLoading() || this.assignments.isLoading(),
  );

  protected readonly errorMessage = computed(
    () => this.trainings.error() ?? this.assignments.error(),
  );

  protected readonly averageLabel = computed(() => {
    const average = this.assignments.averageScore();
    return average === null ? '—' : `${average}`;
  });

  protected readonly needsAttention = computed(() =>
    [...this.assignments.overdue(), ...this.assignments.inProgress()].slice(0, 10),
  );

  constructor() {
    this.reload();
  }

  protected reload(): void {
    this.trainings.loadForActiveOrganization();
    this.assignments.loadForActiveOrganization();
  }
}
