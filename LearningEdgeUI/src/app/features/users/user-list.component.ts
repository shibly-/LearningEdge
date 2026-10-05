import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { CreateUserCommand } from '../../core/models/user';
import { OrganizationContextService } from '../../core/services/organization-context.service';
import { UserStore } from '../../core/stores/user.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { RoleLabelPipe } from '../../shared/pipes/role-label.pipe';
import { UserFormComponent } from './user-form.component';

type RoleFilter = 'all' | 'staff' | 'trainees' | 'admins';

@Component({
  selector: 'app-user-list',
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
  templateUrl: './user-list.component.html',
})
export class UserListComponent {
  protected readonly store = inject(UserStore);

  private readonly context = inject(OrganizationContextService);
  private readonly search = signal('');
  private readonly filter = signal<RoleFilter>('all');
  private readonly creating = signal(false);

  protected readonly searchTerm = this.search.asReadonly();
  protected readonly activeFilter = this.filter.asReadonly();
  protected readonly isCreating = this.creating.asReadonly();
  protected readonly organizationId = this.context.activeOrganizationId;

  protected readonly filters: readonly { readonly id: RoleFilter; readonly label: string }[] = [
    { id: 'all', label: 'Everyone' },
    { id: 'admins', label: 'Admins' },
    { id: 'staff', label: 'Staff' },
    { id: 'trainees', label: 'Trainees' },
  ];

  protected readonly visible = computed(() => {
    const byRole = this.byRole();
    const term = this.search().trim().toLowerCase();
    const filtered =
      term.length === 0
        ? byRole
        : byRole.filter(
            (user) =>
              `${user.firstName} ${user.lastName}`.toLowerCase().includes(term) ||
              user.email.toLowerCase().includes(term),
          );
    return [...filtered].sort((a, b) => a.firstName.localeCompare(b.firstName));
  });

  protected readonly noMatches = computed(
    () => this.store.status() === 'success' && !this.store.isEmpty() && this.visible().length === 0,
  );

  constructor() {
    this.store.loadForActiveOrganization();
  }

  protected setFilter(filter: RoleFilter): void {
    this.filter.set(filter);
  }

  protected onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected openCreate(): void {
    this.creating.set(true);
  }

  protected closeCreate(): void {
    this.creating.set(false);
  }

  protected create(command: CreateUserCommand): void {
    this.store.create(command, () => this.creating.set(false));
  }

  protected reload(): void {
    this.store.loadForActiveOrganization();
  }

  private byRole() {
    switch (this.filter()) {
      case 'staff':
        return this.store.staff();
      case 'trainees':
        return this.store.trainees();
      case 'admins':
        return this.store.admins();
      case 'all':
        return this.store.items();
    }
  }
}
