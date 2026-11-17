import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import type { UpdateUserCommand, UserDto } from '../../core/models/user';
import { UserActivationStatus, activationLabel, activationTone, isPendingActivation } from '../../core/models/user';
import { UserRole } from '../../core/models/user-role';
import { OrganizationStore } from '../../core/stores/organization.store';
import { PlatformUserStore } from '../../core/stores/platform-user.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { RoleLabelPipe } from '../../shared/pipes/role-label.pipe';
import { UserFormComponent } from '../users/user-form.component';

export interface RoleScope {
  readonly id: string;
  readonly label: string;
  readonly roles: readonly UserRole[];
  readonly activationStatus?: UserActivationStatus;
}

/**
 * Cross-organization user table for SysAdmin pages. The organization filter
 * and role scope are applied by the API; search is applied client-side.
 * The organization filter is kept in the `organizationId` query parameter.
 */
@Component({
  selector: 'app-platform-user-directory',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PlatformUserStore],
  imports: [
    RouterLink,
    BadgeComponent,
    RoleLabelPipe,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    ModalShellComponent,
    UserFormComponent,
  ],
  template: `
    <app-page-header [title]="heading()" [subtitle]="subtitle()">
      <ng-content />
    </app-page-header>

    <div class="le-toolbar">
      @if (scopes().length > 1) {
        <div role="group" aria-label="Filter by role" class="filters">
          @for (scope of scopes(); track scope.id) {
            <button
              type="button"
              class="filter"
              [class.is-active]="activeScope().id === scope.id"
              [attr.aria-pressed]="activeScope().id === scope.id"
              (click)="selectScope(scope)"
            >
              {{ scope.label }}
            </button>
          }
        </div>
      }

      <label class="le-visually-hidden" for="directory-org">Filter by organization</label>
      <select
        id="directory-org"
        [disabled]="organizations.isLoading()"
        (change)="onOrganizationChange($event)"
      >
        <option value="" [selected]="organizationId() === null">All organizations</option>
        @for (org of organizations.sortedByName(); track org.id) {
          <option [value]="org.id" [selected]="org.id === organizationId()">
            {{ org.name }}{{ org.isActive ? '' : ' (inactive)' }}
          </option>
        }
      </select>

      <label class="le-visually-hidden" for="directory-status">Filter by status</label>
      <select id="directory-status" [value]="activityFilter()" (change)="onActivity($event)">
        <option value="all">All statuses</option>
        <option value="active">Active</option>
        <option value="inactive">Inactive</option>
      </select>

      <label class="le-visually-hidden" for="directory-search">Search users</label>
      <input
        id="directory-search"
        type="search"
        placeholder="Search by name or email"
        [value]="searchTerm()"
        (input)="onSearch($event)"
      />
    </div>

    @if (store.isLoading()) {
      <div class="le-card" style="padding: 18px">
        <app-skeleton [count]="6" label="Loading users" />
      </div>
    } @else if (store.hasError()) {
      <app-error-state
        title="Could not load users"
        [message]="store.error() ?? ''"
        (retry)="store.reload()"
      />
    } @else if (store.isEmpty()) {
      <app-empty-state title="No users found" [message]="emptyMessage()" />
    } @else if (visible().length === 0) {
      <app-empty-state title="No matches" message="No user matches those filters." />
    } @else {
      <div class="le-card le-table-wrap">
        <table class="le-table">
          <caption>
            {{
              visible().length
            }}
            user(s)
          </caption>
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Email</th>
              <th scope="col">Role</th>
              <th scope="col">Status</th>
              <th scope="col">Organization</th>
              <th scope="col"><span class="le-visually-hidden">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            @for (user of visible(); track user.id) {
              <tr>
                <th scope="row">{{ user.firstName }} {{ user.lastName }}</th>
                <td>{{ user.email }}</td>
                <td>{{ user.role | roleLabel }}</td>
                <td>
                  <app-badge
                    [status]="tone(user.activationStatus)"
                    [text]="activationText(user.activationStatus)"
                  />
                </td>
                <td>
                  <a [routerLink]="['/platform/organizations', user.organizationId]">
                    {{ organizationName(user.organizationId) }}
                  </a>
                </td>
                <td class="row-actions">
                  @if (pending(user.activationStatus)) {
                    <button
                      type="button"
                      class="le-btn le-btn-sm"
                      [disabled]="store.isSaving()"
                      [attr.aria-label]="'Approve ' + user.firstName + ' ' + user.lastName"
                      (click)="approve(user)"
                    >
                      Approve
                    </button>
                  } @else {
                    <button
                      type="button"
                      class="le-btn le-btn-secondary le-btn-sm"
                      [disabled]="store.isSaving()"
                      [attr.aria-label]="
                        (user.activationStatus === activeStatus ? 'Disable ' : 'Enable ') +
                        user.firstName +
                        ' ' +
                        user.lastName
                      "
                      (click)="setEnabled(user)"
                    >
                      {{ user.activationStatus === activeStatus ? 'Disable' : 'Enable' }}
                    </button>
                  }
                  <button
                    type="button"
                    class="le-btn le-btn-secondary"
                    [attr.aria-label]="'Edit ' + user.firstName + ' ' + user.lastName"
                    (click)="editing.set(user)"
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

    @if (editing(); as user) {
      <app-modal-shell title="Edit user" (close)="editing.set(null)">
        <app-user-form
          [organizationId]="user.organizationId"
          [initial]="user"
          [assignableRoles]="adminOnly"
          [saving]="store.isSaving()"
          (save)="update(user.id, $event)"
          (cancel)="editing.set(null)"
        />
      </app-modal-shell>
    }
  `,
})
export class PlatformUserDirectoryComponent implements OnInit {
  readonly heading = input.required<string>();
  readonly subtitle = input<string | null>(null);
  /** The first scope is selected initially. */
  readonly scopes = input.required<readonly RoleScope[]>();
  readonly emptyMessage = input('No users match the selected filters.');

  protected readonly store = inject(PlatformUserStore);
  protected readonly organizations = inject(OrganizationStore);
  /** This directory only edits organization admins, so the role cannot change. */
  protected readonly adminOnly: readonly UserRole[] = [UserRole.OrgAdmin];

  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly search = signal('');
  private readonly activity = signal<'all' | 'active' | 'inactive'>('all');
  private readonly selectedScope = signal<RoleScope | null>(null);
  protected readonly organizationId = signal<string | null>(
    this.route.snapshot.queryParamMap.get('organizationId'),
  );
  protected readonly editing = signal<UserDto | null>(null);

  protected readonly searchTerm = this.search.asReadonly();
  protected readonly activityFilter = this.activity.asReadonly();
  protected readonly activeScope = computed(() => this.selectedScope() ?? this.scopes()[0]);
  protected readonly activationText = activationLabel;
  protected readonly tone = activationTone;
  protected readonly pending = isPendingActivation;
  protected readonly activeStatus = UserActivationStatus.Active;

  protected readonly visible = computed(() => {
    const term = this.search().trim().toLowerCase();
    const activity = this.activity();
    return this.store.items().filter((user) => {
      const matchesActivity =
        activity === 'all' ||
        (activity === 'active'
          ? user.activationStatus === UserActivationStatus.Active
          : user.activationStatus === UserActivationStatus.Inactive);
      const matchesTerm =
        term.length === 0 ||
        `${user.firstName} ${user.lastName}`.toLowerCase().includes(term) ||
        user.email.toLowerCase().includes(term);
      return matchesActivity && matchesTerm;
    });
  });

  ngOnInit(): void {
    if (this.organizations.status() === 'idle') {
      this.organizations.loadAll();
    }
    this.refresh();
  }

  protected organizationName(id: string): string {
    return this.organizations.findById(id)?.name ?? 'Unknown organization';
  }

  protected selectScope(scope: RoleScope): void {
    this.selectedScope.set(scope);
    this.refresh();
  }

  protected onOrganizationChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.organizationId.set(value.length > 0 ? value : null);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { organizationId: this.organizationId() },
      replaceUrl: true,
    });
    this.refresh();
  }

  protected onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onActivity(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.activity.set(value === 'active' || value === 'inactive' ? value : 'all');
  }

  protected update(id: string, command: UpdateUserCommand): void {
    this.store.update(id, command, () => this.editing.set(null));
  }

  protected approve(user: UserDto): void {
    this.store.approve(user);
  }

  protected setEnabled(user: UserDto): void {
    this.store.setEnabled(user, user.activationStatus !== UserActivationStatus.Active);
  }

  private refresh(): void {
    this.store.loadWith({
      organizationId: this.organizationId(),
      roles: this.activeScope().roles,
      activationStatus: this.activeScope().activationStatus,
    });
  }
}
