import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map, type Subscription } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import type { UpdateUserCommand, UserDto } from '../../core/models/user';
import { UserRepository } from '../../core/services/user.repository';
import { describeError } from '../../core/stores/async-collection.store';
import { UserStore } from '../../core/stores/user.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { RoleLabelPipe } from '../../shared/pipes/role-label.pipe';
import { UserFormComponent } from './user-form.component';

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
    ModalShellComponent,
    UserFormComponent,
  ],
  template: `
    <app-page-header [title]="headingText()" subtitle="User detail">
      <a class="le-btn le-btn-secondary" routerLink="/users">Back to users</a>
      @if (state() === 'loaded') {
        <button type="button" class="le-btn" (click)="editing.set(true)">Edit</button>
      }
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

    @if (editing() && user(); as current) {
      <app-modal-shell title="Edit user" (close)="editing.set(false)">
        <app-user-form
          [initial]="current"
          [organizationId]="current.organizationId"
          [saving]="store.isSaving()"
          (save)="saveEdit(current.id, $event)"
          (cancel)="editing.set(false)"
        />
      </app-modal-shell>
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
  protected readonly store = inject(UserStore);

  protected readonly editing = signal(false);
  private request: Subscription | null = null;

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
    // The router reuses this component across ids, so reload on every change.
    effect(() => {
      this.userId();
      untracked(() => this.load());
    });
    this.destroyRef.onDestroy(() => this.request?.unsubscribe());
  }

  protected headingText(): string {
    const user = this.userState();
    return user === null ? 'User' : `${user.firstName} ${user.lastName}`.trim();
  }

  protected saveEdit(id: string, command: UpdateUserCommand): void {
    this.store.update(id, command, (user) => {
      this.userState.set(user);
      this.editing.set(false);
    });
  }

  protected load(): void {
    this.request?.unsubscribe();
    this.editing.set(false);

    const id = this.userId();
    if (id === null) {
      this.viewState.set('missing');
      return;
    }

    this.viewState.set('loading');
    this.failure.set(null);

    this.request = this.repository.getById(id).subscribe({
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
