import { Injectable, inject } from '@angular/core';
import { Observable, delay, forkJoin, map, of, switchMap, throwError } from 'rxjs';
import { ApiService } from '../http/api.service';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { Category } from '../models/category';
import {
  validateTrainingFiles,
  type CreateTrainingCommand,
  type Training,
  type TrainingFile,
  type UpdateTrainingCommand,
} from '../models/training';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS } from './repository-support';

export interface UploadTrainingFilesRequest {
  readonly organizationId: string;
  readonly categoryId: string;
  readonly trainingId: string;
  readonly uploadedByUserId: string;
  readonly files: readonly File[];
}

export interface RemoveTrainingFileRequest {
  readonly organizationId: string;
  readonly categoryId: string;
  readonly trainingId: string;
  readonly fileId: string;
  readonly removedByUserId: string;
}

export abstract class TrainingRepository {
  abstract listByOrganization(organizationId: string): Observable<readonly Training[]>;
  abstract listByCategory(
    organizationId: string,
    categoryId: string,
  ): Observable<readonly Training[]>;
  abstract getById(
    organizationId: string,
    categoryId: string,
    id: string,
  ): Observable<Training | null>;
  abstract create(command: CreateTrainingCommand): Observable<string>;
  abstract update(command: UpdateTrainingCommand): Observable<Training>;
  abstract uploadFiles(request: UploadTrainingFilesRequest): Observable<readonly TrainingFile[]>;
  abstract removeFile(request: RemoveTrainingFileRequest): Observable<void>;
}

/** TrainingDTO as the API returns it: no organizationId. */
type TrainingDto = Omit<Training, 'organizationId'>;

@Injectable()
export class HttpTrainingRepository extends TrainingRepository {
  private readonly api = inject(ApiService);

  /** There is no organization-wide training endpoint, so this fans out over the categories. */
  override listByOrganization(organizationId: string): Observable<readonly Training[]> {
    return this.api.get<readonly Category[]>(apiPaths.category.list(organizationId)).pipe(
      switchMap((categories) =>
        categories.length === 0
          ? of([] as (readonly Training[])[])
          : forkJoin(categories.map((c) => this.listByCategory(organizationId, c.id))),
      ),
      map((groups) => groups.flat()),
    );
  }

  override listByCategory(
    organizationId: string,
    categoryId: string,
  ): Observable<readonly Training[]> {
    return this.api
      .get<readonly TrainingDto[]>(apiPaths.training.list(categoryId))
      .pipe(map((items) => items.map((dto) => ({ ...dto, organizationId }))));
  }

  override getById(
    organizationId: string,
    categoryId: string,
    id: string,
  ): Observable<Training | null> {
    return this.api
      .getOptional<TrainingDto>(apiPaths.training.byId(categoryId, id))
      .pipe(map((dto) => (dto === null ? null : { ...dto, organizationId })));
  }

  override create(command: CreateTrainingCommand): Observable<string> {
    const { organizationId: _organizationId, categoryId, ...body } = command;
    return this.api.post(apiPaths.training.list(categoryId), body);
  }

  override update(command: UpdateTrainingCommand): Observable<Training> {
    const { organizationId, categoryId, id, ...body } = command;
    return this.api
      .put<typeof body, TrainingDto>(apiPaths.training.byId(categoryId, id), body)
      .pipe(map((dto) => ({ ...dto, organizationId })));
  }

  override uploadFiles(request: UploadTrainingFilesRequest): Observable<readonly TrainingFile[]> {
    const form = new FormData();
    form.append('uploadedByUserId', request.uploadedByUserId);
    for (const file of request.files) {
      form.append('files', file, file.name);
    }
    return this.api.postForm<readonly TrainingFile[]>(
      apiPaths.training.files(request.categoryId, request.trainingId),
      form,
    );
  }

  override removeFile(request: RemoveTrainingFileRequest): Observable<void> {
    const url = `${apiPaths.training.file(request.categoryId, request.trainingId, request.fileId)}?removedByUserId=${encodeURIComponent(request.removedByUserId)}`;
    return this.api.delete(url);
  }
}

const CONTENT_TYPES: Readonly<Record<string, string>> = {
  pdf: 'application/pdf',
  docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  txt: 'text/plain',
};

@Injectable()
export class MockTrainingRepository extends TrainingRepository {
  private readonly db = inject(MockDb);

  override listByOrganization(organizationId: string): Observable<readonly Training[]> {
    const items = this.db.trainings.filter((t) => t.organizationId === organizationId);
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override listByCategory(
    organizationId: string,
    categoryId: string,
  ): Observable<readonly Training[]> {
    const items = this.db.trainings.filter(
      (t) => t.organizationId === organizationId && t.categoryId === categoryId,
    );
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override getById(
    organizationId: string,
    categoryId: string,
    id: string,
  ): Observable<Training | null> {
    const found =
      this.db.trainings.find(
        (t) => t.id === id && t.categoryId === categoryId && t.organizationId === organizationId,
      ) ?? null;
    return of(found).pipe(delay(MOCK_LATENCY_MS));
  }

  override create(command: CreateTrainingCommand): Observable<string> {
    if (!this.db.categories.some((c) => c.id === command.categoryId)) {
      return throwError(
        () => new ApiFailure('not-found', `No category found with Id ${command.categoryId}.`, 404),
      );
    }
    const problem = this.validate(command, null);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const id = this.db.nextId('trn');
    this.db.trainings = [
      ...this.db.trainings,
      {
        id,
        organizationId: command.organizationId,
        categoryId: command.categoryId,
        name: command.name.trim(),
        description: command.description.trim(),
        isActive: command.isActive,
        files: [],
      },
    ];
    return of(id).pipe(delay(MOCK_LATENCY_MS));
  }

  override update(command: UpdateTrainingCommand): Observable<Training> {
    const existing = this.db.trainings.find(
      (t) => t.id === command.id && t.categoryId === command.categoryId,
    );
    if (existing === undefined) {
      return throwError(
        () => new ApiFailure('not-found', `No training found with Id ${command.id}.`, 404),
      );
    }
    const problem = this.validate(command, command.id);
    if (problem !== null) {
      return throwError(() => problem);
    }

    const updated: Training = {
      ...existing,
      name: command.name.trim(),
      description: command.description.trim(),
      isActive: command.isActive,
    };
    this.replace(updated);
    return of(updated).pipe(delay(MOCK_LATENCY_MS));
  }

  override uploadFiles(request: UploadTrainingFilesRequest): Observable<readonly TrainingFile[]> {
    const training = this.db.trainings.find(
      (t) => t.id === request.trainingId && t.categoryId === request.categoryId,
    );
    if (training === undefined) {
      return throwError(
        () => new ApiFailure('not-found', `No training found with Id ${request.trainingId}.`, 404),
      );
    }
    const invalid = validateTrainingFiles(request.files);
    if (invalid !== null) {
      return throwError(() => new ApiFailure('validation', invalid, 400));
    }

    const now = new Date().toISOString();
    const added: TrainingFile[] = request.files.map((file) => ({
      id: this.db.nextGuid(),
      fileName: file.name,
      contentType: CONTENT_TYPES[file.name.split('.').pop()?.toLowerCase() ?? ''] ?? 'text/plain',
      sizeBytes: file.size,
      uploadedByUserId: request.uploadedByUserId,
      createdAt: now,
    }));
    this.replace({ ...training, files: [...training.files, ...added] });
    return of(added).pipe(delay(MOCK_LATENCY_MS));
  }

  override removeFile(request: RemoveTrainingFileRequest): Observable<void> {
    const training = this.db.trainings.find(
      (t) =>
        t.id === request.trainingId &&
        t.categoryId === request.categoryId &&
        t.organizationId === request.organizationId,
    );
    if (training === undefined) {
      return throwError(
        () => new ApiFailure('not-found', `No training found with Id ${request.trainingId}.`, 404),
      );
    }
    if (!training.files.some((file) => file.id === request.fileId)) {
      return throwError(
        () => new ApiFailure('not-found', `No file found with Id ${request.fileId}.`, 404),
      );
    }

    this.replace({
      ...training,
      files: training.files.filter((file) => file.id !== request.fileId),
    });
    return of(undefined).pipe(delay(MOCK_LATENCY_MS));
  }

  private replace(training: Training): void {
    this.db.trainings = this.db.trainings.map((t) => (t.id === training.id ? training : t));
  }

  private validate(command: CreateTrainingCommand, ownId: string | null): ApiFailure | null {
    const name = command.name.trim();
    if (name.length === 0) {
      return new ApiFailure('validation', 'Training name is required.', 400);
    }
    const taken = this.db.trainings.some(
      (t) =>
        t.id !== ownId &&
        t.categoryId === command.categoryId &&
        t.name.toLowerCase() === name.toLowerCase(),
    );
    return taken
      ? new ApiFailure('conflict', `A training named ${name} already exists in this category.`, 409)
      : null;
  }
}
