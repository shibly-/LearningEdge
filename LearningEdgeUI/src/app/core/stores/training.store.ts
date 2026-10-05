import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { CreateTrainingCommand, Training } from '../models/training';
import { OrganizationContextService } from '../services/organization-context.service';
import { ToastService } from '../services/toast.service';
import { TrainingRepository } from '../services/training.repository';
import { AsyncCollectionStore, describeError } from './async-collection.store';

@Injectable({ providedIn: 'root' })
export class TrainingStore extends AsyncCollectionStore<Training> {
  private readonly repository = inject(TrainingRepository);
  private readonly context = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);

  private readonly savingState = signal(false);
  readonly isSaving = this.savingState.asReadonly();

  readonly published = computed(() => this.items().filter((t) => t.status === 'published'));
  readonly drafts = computed(() => this.items().filter((t) => t.status === 'draft'));

  loadForActiveOrganization(): void {
    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      this.reset();
      return;
    }
    this.load(this.repository.listByOrganization(organizationId));
  }

  loadForCategory(categoryId: string): void {
    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      this.reset();
      return;
    }
    this.load(this.repository.listByCategory(organizationId, categoryId));
  }

  create(command: CreateTrainingCommand, onCreated: (id: string) => void): void {
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
        next: (id) => {
          this.toast.success(`Training "${command.title}" created as a draft.`);
          this.loadForActiveOrganization();
          onCreated(id);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  findById(id: string): Training | undefined {
    return this.items().find((training) => training.id === id);
  }
}
