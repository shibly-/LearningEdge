import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type {
  CreateTrainingCommand,
  Training,
  TrainingFile,
  UpdateTrainingCommand,
} from '../models/training';
import { OrganizationContextService } from '../services/organization-context.service';
import { ToastService } from '../services/toast.service';
import {
  TrainingRepository,
  type UploadTrainingFilesRequest,
} from '../services/training.repository';
import { AsyncCollectionStore, describeError } from './async-collection.store';

@Injectable({ providedIn: 'root' })
export class TrainingStore extends AsyncCollectionStore<Training> {
  private readonly repository = inject(TrainingRepository);
  private readonly context = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);

  private readonly savingState = signal(false);
  readonly isSaving = this.savingState.asReadonly();

  private readonly uploadingState = signal(false);
  readonly isUploading = this.uploadingState.asReadonly();

  readonly active = computed(() => this.items().filter((t) => t.isActive));
  readonly inactive = computed(() => this.items().filter((t) => !t.isActive));

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
          this.toast.success(`Training "${command.name}" created.`);
          this.loadForActiveOrganization();
          onCreated(id);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  update(command: UpdateTrainingCommand, onUpdated: (training: Training) => void): void {
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
        next: (training) => {
          this.toast.success(`Training "${training.name}" updated.`);
          this.upsert(training, (existing) => existing.id === training.id);
          onUpdated(training);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  uploadFiles(
    request: UploadTrainingFilesRequest,
    onUploaded: (files: readonly TrainingFile[]) => void,
  ): void {
    if (this.uploadingState()) {
      return;
    }
    this.uploadingState.set(true);

    this.repository
      .uploadFiles(request)
      .pipe(
        finalize(() => this.uploadingState.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (files) => {
          const count = files.length === 1 ? '1 file' : `${files.length} files`;
          this.toast.success(`Uploaded ${count}.`);
          const training = this.findById(request.trainingId);
          if (training !== undefined) {
            this.upsert(
              { ...training, files: [...training.files, ...files] },
              (existing) => existing.id === training.id,
            );
          }
          onUploaded(files);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  findById(id: string): Training | undefined {
    return this.items().find((training) => training.id === id);
  }
}
