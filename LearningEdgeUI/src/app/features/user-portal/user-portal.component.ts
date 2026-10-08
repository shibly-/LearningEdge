import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../core/auth/auth.service';
import { AssignmentStore } from '../../core/stores/assignment.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { StatCardComponent } from '../../shared/components/stat-card.component';

/** Learner scope: only the signed-in trainee's own assignments and scores. */
@Component({
  selector: 'app-user-portal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    BadgeComponent,
    PageHeaderComponent,
    StatCardComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  template: `
    <app-page-header [title]="'My training'" subtitle="Your assigned courses and results." />

    @if (userId() === null) {
      <app-empty-state
        title="Not signed in"
        message="Sign in again to see your assigned training."
      />
    } @else if (store.isLoading()) {
      <div class="le-card" style="padding: 18px">
        <app-skeleton [count]="5" label="Loading your training" />
      </div>
    } @else if (store.hasError()) {
      <app-error-state [message]="store.error() ?? ''" (retry)="reload()" />
    } @else {
      <div class="le-grid-stats">
        <app-stat-card label="Assigned" [value]="store.count()" />
        <app-stat-card label="Completed" [value]="store.completed().length" />
        <app-stat-card label="Overdue" [value]="store.overdue().length" />
        <app-stat-card label="Average score" [value]="averageLabel()" />
      </div>

      @if (store.isEmpty()) {
        <app-empty-state
          title="No training assigned"
          message="When your organization assigns a course, it will appear here."
        />
      } @else {
        <section class="le-card le-table-wrap block">
          <table class="le-table">
            <caption>
              Assigned training
            </caption>
            <thead>
              <tr>
                <th scope="col">Training</th>
                <th scope="col">Status</th>
                <th scope="col">Due</th>
              </tr>
            </thead>
            <tbody>
              @for (assignment of store.items(); track assignment.id) {
                <tr>
                  <th scope="row">{{ assignment.trainingTitle }}</th>
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

      @if (store.results().length > 0) {
        <section class="le-card le-table-wrap block">
          <table class="le-table">
            <caption>
              Scores
            </caption>
            <thead>
              <tr>
                <th scope="col">Training</th>
                <th scope="col">Score</th>
                <th scope="col">Pass mark</th>
                <th scope="col">Outcome</th>
                <th scope="col">Completed</th>
              </tr>
            </thead>
            <tbody>
              @for (result of store.results(); track result.id) {
                <tr>
                  <th scope="row">{{ result.trainingTitle }}</th>
                  <td>{{ result.score }}</td>
                  <td>{{ result.passMark }}</td>
                  <td>
                    <app-badge
                      [status]="result.passed ? 'passed' : 'fail'"
                      [text]="result.passed ? 'Passed' : 'Not passed'"
                    />
                  </td>
                  <td>{{ result.completedAt | date: 'mediumDate' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </section>
      }
    }
  `,
  styles: `
    .block {
      margin-bottom: 18px;
    }
  `,
})
export class UserPortalComponent {
  protected readonly store = inject(AssignmentStore);

  private readonly auth = inject(AuthService);

  protected readonly userId = computed(() => this.auth.currentUser()?.id ?? null);

  protected readonly averageLabel = computed(() => {
    const average = this.store.averageScore();
    return average === null ? '—' : `${average}`;
  });

  constructor() {
    this.reload();
  }

  protected reload(): void {
    const userId = this.userId();
    if (userId !== null) {
      this.store.loadForTrainee(userId);
    }
  }
}
