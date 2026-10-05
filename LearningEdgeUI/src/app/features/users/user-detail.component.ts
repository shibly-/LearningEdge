import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import type { UserDto } from '../../core/models/user';
import { UserRepository } from '../../core/services/user.repository';
import { describeError } from '../../core/stores/async-collection.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { RoleLabelPipe } from '../../shared/pipes/role-label.pipe';

type ViewState = 'loading' | 'loaded' | 'missing' | 'error';

/**
 * Backed by GET /api/v1/user/{id} — a live, rate-limited endpoint. The retry
 * interceptor absorbs the 503s it emits when the window is exhausted.
 */
@Component({
  selector: 'app-user-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    RoleLabelPipe,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  template: `
    <app-page-header [title]="headingText()" subtitle="User detail">
      <a class="le-btn le-btn-secondary" routerLink="/users">Back to users</a>
    </app-page-header>

    @switch (state()) {
      @case ('loading') {
        <div class="le-card" style="padding: 18px">
          <app-skeleton [count]="4" label="Loading user" />
        </div>
      }
      @case ('error') {
        <app-error-state [message]="errorMessage() ?? ''" (retry)="load()" />
      }
      @case ('missing') {
        <app-empty-state
          title="User not found"
          message="The record may have been removed, or the identifier in the URL is incorrect."
        />
      }
      @case ('loaded') {
        <dl class="le-card detail">
          <dt>Name</dt>
          <dd>{{ user()?.firstName }} {{ user()?.lastName }}</dd>
          <dt>Email</dt>
          <dd>{{ user()?.email }}</dd>
          <dt>Role</dt>
          <dd>{{ user()?.role | roleLabel }}</dd>
          <dt>Organization</dt>
          <dd>
            <code>{{ user()?.organizationId }}</code>
          </dd>
        </dl>
      }
    }
  `,
  styles: `
    .detail {
      display: grid;
      grid-template-columns: 180px 1fr;
      margin: 0;
      padding: 4px 0;
    }

    dt,
    dd {
      margin: 0;
      padding: 14px 18px;
      border-bottom: 1px solid var(--le-border);
    }

    dt {
      font-weight: 600;
      color: var(--le-text-muted);
    }

    .detail > :nth-last-child(1),
    .detail > :nth-last-child(2) {
      border-bottom: 0;
    }

    @media (max-width: 640px) {
      .detail {
        grid-template-columns: 1fr;
      }

      dt {
        padding-bottom: 0;
        border-bottom: 0;
      }
    }
  `,
})
export class UserDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly repository = inject(UserRepository);
  private readonly destroyRef = inject(DestroyRef);

  private readonly userState = signal<UserDto | null>(null);
  private readonly viewState = signal<ViewState>('loading');
  private readonly failure = signal<string | null>(null);

  protected readonly user = this.userState.asReadonly();
  protected readonly state = this.viewState.asReadonly();
  protected readonly errorMessage = this.failure.asReadonly();

  private readonly userId = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('userId'))),
    { initialValue: null },
  );

  constructor() {
    this.load();
  }

  protected headingText(): string {
    const user = this.userState();
    return user === null ? 'User' : `${user.firstName} ${user.lastName}`.trim();
  }

  protected load(): void {
    const id = this.userId();
    if (id === null) {
      this.viewState.set('missing');
      return;
    }

    this.viewState.set('loading');
    this.failure.set(null);

    this.repository
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (user) => {
          this.userState.set(user);
          this.viewState.set(user === null ? 'missing' : 'loaded');
        },
        error: (error: unknown) => {
          this.failure.set(describeError(error));
          this.viewState.set('error');
        },
      });
  }
}
