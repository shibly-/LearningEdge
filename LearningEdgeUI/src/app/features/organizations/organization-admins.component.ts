import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import type { Subscription } from 'rxjs';
import type { UpdateUserCommand, UserDto } from '../../core/models/user';
import { UserRole } from '../../core/models/user-role';
import { UserRepository } from '../../core/services/user.repository';
import { ToastService } from '../../core/services/toast.service';
import { describeError } from '../../core/stores/async-collection.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { UserFormComponent } from '../users/user-form.component';

type LoadState = 'loading' | 'ready' | 'error';

/** Organization admins of one tenant. Creating one goes through the Keycloak-backed form. */
@Component({
  selector: 'app-organization-admins',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    ModalShellComponent,
    UserFormComponent,
  ],
  template: `
    <section class="admins" aria-labelledby="org-admins-heading">
      <div class="heading">
        <h2 id="org-admins-heading">Administrators</h2>
        <a
          class="le-btn"
          routerLink="/platform/org-admins/new"
          [queryParams]="{ organizationId: organizationId() }"
        >
          Add administrator
        </a>
      </div>

      @switch (state()) {
        @case ('loading') {
          <div class="le-card" style="padding: 18px">
            <app-skeleton [count]="3" label="Loading administrators" />
          </div>
        }
        @case ('error') {
          <app-error-state [message]="errorMessage() ?? ''" (retry)="load()" />
        }
        @case ('ready') {
          @if (admins().length === 0) {
            <app-empty-state
              title="No administrators yet"
              message="Add an organization admin so this tenant can manage its own users."
            />
          } @else {
            <div class="le-card le-table-wrap">
              <table class="le-table">
                <caption>
                  Organization administrators
                </caption>
                <thead>
                  <tr>
                    <th scope="col">Name</th>
                    <th scope="col">Email</th>
                    <th scope="col"><span class="le-visually-hidden">Actions</span></th>
                  </tr>
                </thead>
                <tbody>
                  @for (admin of admins(); track admin.id) {
                    <tr>
                      <th scope="row">{{ admin.firstName }} {{ admin.lastName }}</th>
                      <td>{{ admin.email }}</td>
                      <td class="row-actions">
                        <button
                          type="button"
                          class="le-btn le-btn-secondary"
                          [attr.aria-label]="'Edit ' + admin.firstName + ' ' + admin.lastName"
                          (click)="editing.set(admin)"
                        >
                          Edit
                        </button>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        }
      }
    </section>

    @if (editing(); as admin) {
      <app-modal-shell title="Edit administrator" (close)="editing.set(null)">
        <app-user-form
          [organizationId]="admin.organizationId"
          [initial]="admin"
          [assignableRoles]="adminOnly"
          [saving]="isSaving()"
          (save)="save(admin.id, $event)"
          (cancel)="editing.set(null)"
        />
      </app-modal-shell>
    }
  `,
  styles: `
    .admins {
      margin-top: 22px;
    }

    .heading {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      margin-bottom: 12px;
    }

    h2 {
      margin: 0;
      font-size: 1.15rem;
    }
  `,
})
export class OrganizationAdminsComponent {
  readonly organizationId = input.required<string>();

  private readonly users = inject(UserRepository);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private request: Subscription | null = null;

  private readonly adminState = signal<readonly UserDto[]>([]);
  private readonly viewState = signal<LoadState>('loading');
  private readonly failure = signal<string | null>(null);
  private readonly saving = signal(false);

  protected readonly admins = this.adminState.asReadonly();
  protected readonly state = this.viewState.asReadonly();
  protected readonly errorMessage = this.failure.asReadonly();
  protected readonly isSaving = this.saving.asReadonly();
  protected readonly editing = signal<UserDto | null>(null);
  protected readonly adminOnly: readonly UserRole[] = [UserRole.OrgAdmin];

  constructor() {
    effect(() => {
      this.organizationId();
      untracked(() => this.load());
    });
    this.destroyRef.onDestroy(() => this.request?.unsubscribe());
  }

  protected load(): void {
    this.request?.unsubscribe();
    const organizationId = this.organizationId();
    this.viewState.set('loading');
    this.failure.set(null);
    this.editing.set(null);

    this.request = this.users
      .listAll({ organizationId, roles: [UserRole.OrgAdmin] })
      .subscribe({
        next: (admins) => {
          this.adminState.set(admins);
          this.viewState.set('ready');
        },
        error: (error: unknown) => {
          this.failure.set(describeError(error));
          this.viewState.set('error');
        },
      });
  }

  protected save(id: string, command: UpdateUserCommand): void {
    if (this.saving()) {
      return;
    }
    this.saving.set(true);

    this.users
      .update(id, command)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.saving.set(false);
          this.adminState.update((admins) =>
            admins.map((admin) => (admin.id === updated.id ? updated : admin)),
          );
          this.editing.set(null);
          this.toast.success(`${updated.firstName} ${updated.lastName} updated.`.trim());
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.toast.error(describeError(error));
        },
      });
  }
}
