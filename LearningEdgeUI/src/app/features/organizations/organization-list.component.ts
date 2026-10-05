import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { CreateOrganizationCommand } from '../../core/models/organization';
import { OrganizationStore } from '../../core/stores/organization.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { OrganizationFormComponent } from './organization-form.component';

@Component({
  selector: 'app-organization-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    ModalShellComponent,
    OrganizationFormComponent,
  ],
  templateUrl: './organization-list.component.html',
})
export class OrganizationListComponent {
  protected readonly store = inject(OrganizationStore);

  private readonly search = signal('');
  private readonly creating = signal(false);

  protected readonly searchTerm = this.search.asReadonly();
  protected readonly isCreating = this.creating.asReadonly();

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const all = this.store.sortedByName();
    if (term.length === 0) {
      return all;
    }
    return all.filter(
      (org) =>
        org.name.toLowerCase().includes(term) || org.description.toLowerCase().includes(term),
    );
  });

  protected readonly noMatches = computed(
    () =>
      this.store.status() === 'success' && !this.store.isEmpty() && this.filtered().length === 0,
  );

  constructor() {
    this.store.loadAll();
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

  protected create(command: CreateOrganizationCommand): void {
    this.store.create(command, () => this.creating.set(false));
  }

  protected reload(): void {
    this.store.loadAll();
  }
}
