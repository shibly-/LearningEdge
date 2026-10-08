import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type {
  CreateOrganizationCommand,
  OrganizationDto,
  UpdateOrganizationCommand,
} from '../models/organization';
import { OrganizationRepository } from '../services/organization.repository';
import { ToastService } from '../services/toast.service';
import { AsyncCollectionStore, describeError } from './async-collection.store';

/** Platform scope: only SysAdmin reaches this data. */
@Injectable({ providedIn: 'root' })
export class OrganizationStore extends AsyncCollectionStore<OrganizationDto> {
  private readonly repository = inject(OrganizationRepository);
  private readonly toast = inject(ToastService);

  private readonly savingState = signal(false);
  readonly isSaving = this.savingState.asReadonly();

  readonly sortedByName = computed(() =>
    [...this.items()].sort((a, b) => a.name.localeCompare(b.name)),
  );

  loadAll(): void {
    this.load(this.repository.list());
  }

  create(command: CreateOrganizationCommand, onCreated: (id: string) => void): void {
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
          this.toast.success(`Organization "${command.name}" created.`);
          this.loadAll();
          onCreated(id);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  update(
    id: string,
    command: UpdateOrganizationCommand,
    onUpdated: (organization: OrganizationDto) => void,
  ): void {
    if (this.savingState()) {
      return;
    }
    this.savingState.set(true);

    this.repository
      .update(id, command)
      .pipe(
        finalize(() => this.savingState.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (organization) => {
          this.toast.success(`Organization "${organization.name}" updated.`);
          this.upsert(organization, (existing) => existing.id === organization.id);
          onUpdated(organization);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  findById(id: string): OrganizationDto | undefined {
    return this.items().find((org) => org.id === id);
  }
}
