import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Category, CreateCategoryCommand, UpdateCategoryCommand } from '../models/category';
import { CategoryRepository } from '../services/category.repository';
import { OrganizationContextService } from '../services/organization-context.service';
import { ToastService } from '../services/toast.service';
import { AsyncCollectionStore, describeError } from './async-collection.store';

@Injectable({ providedIn: 'root' })
export class CategoryStore extends AsyncCollectionStore<Category> {
  private readonly repository = inject(CategoryRepository);
  private readonly context = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);

  private readonly savingState = signal(false);
  readonly isSaving = this.savingState.asReadonly();

  readonly sortedByName = computed(() =>
    [...this.items()].sort((a, b) => a.name.localeCompare(b.name)),
  );
  readonly active = computed(() => this.sortedByName().filter((c) => c.isActive));

  loadForActiveOrganization(): void {
    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      this.reset();
      return;
    }
    this.load(this.repository.listByOrganization(organizationId));
  }

  create(command: CreateCategoryCommand, onCreated: () => void): void {
    if (this.savingState()) {
      return;
    }
    this.savingState.set(true);

    this.repository
      .create(command)
      .pipe(
        finalize(() => this.savingState.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success(`Category "${command.name}" created.`);
          this.loadForActiveOrganization();
          onCreated();
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  update(command: UpdateCategoryCommand, onUpdated: (category: Category) => void): void {
    if (this.savingState()) {
      return;
    }
    this.savingState.set(true);

    this.repository
      .update(command)
      .pipe(
        finalize(() => this.savingState.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (category) => {
          this.toast.success(`Category "${category.name}" updated.`);
          this.upsert(category, (existing) => existing.id === category.id);
          onUpdated(category);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  findById(id: string): Category | undefined {
    return this.items().find((category) => category.id === id);
  }
}
