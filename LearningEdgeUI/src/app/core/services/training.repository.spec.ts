import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { Category } from '../models/category';
import type { Training } from '../models/training';
import { MockDb } from './mock/mock-db';
import { HttpTrainingRepository, MockTrainingRepository } from './training.repository';

const ORG = 'org-1';

const category: Category = {
  id: 'cat-1',
  organizationId: ORG,
  name: 'Safety',
  description: '',
  isActive: true,
};

const training: Training = {
  id: 'trn-1',
  organizationId: ORG,
  categoryId: 'cat-1',
  name: 'Forklift Safety',
  description: '',
  isActive: true,
  files: [],
};

function file(name: string, size = 10): File {
  return new File(['x'.repeat(size)], name);
}

describe('HttpTrainingRepository', () => {
  let repository: HttpTrainingRepository;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [HttpTrainingRepository, provideHttpClient(), provideHttpClientTesting()],
    });
    repository = TestBed.inject(HttpTrainingRepository);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('lists an organization by fanning out over its categories', async () => {
    const result = firstValueFrom(repository.listByOrganization(ORG));

    http.expectOne(apiPaths.category.list(ORG)).flush([category, { ...category, id: 'cat-2' }]);
    const { organizationId: _a, ...dto } = training;
    http.expectOne(apiPaths.training.list('cat-1')).flush([dto]);
    http
      .expectOne(apiPaths.training.list('cat-2'))
      .flush([{ ...dto, id: 'trn-2', categoryId: 'cat-2' }]);

    const items = await result;
    expect(items.map((t) => t.id)).toEqual(['trn-1', 'trn-2']);
    expect(items.every((t) => t.organizationId === ORG)).toBe(true);
  });

  it('returns an empty list without further requests when there are no categories', async () => {
    const result = firstValueFrom(repository.listByOrganization(ORG));
    http.expectOne(apiPaths.category.list(ORG)).flush([]);
    expect(await result).toEqual([]);
  });

  it('sends only the fields the API accepts on create', async () => {
    const result = firstValueFrom(
      repository.create({
        organizationId: ORG,
        categoryId: 'cat-1',
        name: 'New',
        description: 'd',
        isActive: false,
      }),
    );

    const request = http.expectOne(apiPaths.training.list('cat-1'));
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'New', description: 'd', isActive: false });
    request.flush('new-id', { status: 201, statusText: 'Created' });

    expect(await result).toBe('new-id');
  });

  it('uploads files as multipart with the uploader id', async () => {
    const result = firstValueFrom(
      repository.uploadFiles({
        organizationId: ORG,
        categoryId: 'cat-1',
        trainingId: 'trn-1',
        uploadedByUserId: 'user-1',
        files: [file('a.pdf'), file('b.txt')],
      }),
    );

    const request = http.expectOne(apiPaths.training.files('cat-1', 'trn-1'));
    const body = request.request.body as FormData;
    expect(body.get('uploadedByUserId')).toBe('user-1');
    expect((body.getAll('files') as File[]).map((f) => f.name)).toEqual(['a.pdf', 'b.txt']);
    request.flush([]);

    await result;
  });

  it('maps a 403 ProblemDetails to a forbidden failure carrying its detail', async () => {
    const result = firstValueFrom(
      repository.uploadFiles({
        organizationId: ORG,
        categoryId: 'cat-1',
        trainingId: 'trn-1',
        uploadedByUserId: 'user-1',
        files: [file('a.pdf')],
      }),
    );

    http
      .expectOne(apiPaths.training.files('cat-1', 'trn-1'))
      .flush(
        { title: 'Forbidden', detail: 'Only admins can upload files.' },
        { status: 403, statusText: 'Forbidden' },
      );

    await expect(result).rejects.toMatchObject({
      kind: 'forbidden',
      message: 'Only admins can upload files.',
    });
  });
});

describe('MockTrainingRepository', () => {
  let repository: MockTrainingRepository;
  let db: MockDb;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({ providers: [MockTrainingRepository] });
    repository = TestBed.inject(MockTrainingRepository);
    db = TestBed.inject(MockDb);
    db.categories = [category];
    db.trainings = [training];
  });

  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  async function settle<T>(source: Observable<T>): Promise<T> {
    const result = firstValueFrom(source);
    // Validation failures reject synchronously; mark handled until the caller's expect attaches.
    result.catch(() => undefined);
    await vi.advanceTimersByTimeAsync(1000);
    return result;
  }

  it('rejects a duplicate name in the same category with a conflict', async () => {
    await expect(
      settle(
        repository.create({
          organizationId: ORG,
          categoryId: 'cat-1',
          name: 'forklift safety',
          description: '',
          isActive: true,
        }),
      ),
    ).rejects.toMatchObject({ kind: 'conflict' });
  });

  it('updates in place and keeps the existing files', async () => {
    db.trainings = [{ ...training, files: [{ ...sampleFile }] }];

    const updated = await settle(
      repository.update({ ...training, name: 'Renamed', isActive: false }),
    );

    expect(updated.name).toBe('Renamed');
    expect(updated.isActive).toBe(false);
    expect(updated.files).toHaveLength(1);
    expect(db.trainings[0].name).toBe('Renamed');
  });

  it('appends uploaded file metadata to the training', async () => {
    const added = await settle(
      repository.uploadFiles({
        organizationId: ORG,
        categoryId: 'cat-1',
        trainingId: 'trn-1',
        uploadedByUserId: 'user-1',
        files: [file('guide.docx', 2048)],
      }),
    );

    expect(added).toHaveLength(1);
    expect(added[0].fileName).toBe('guide.docx');
    expect(added[0].sizeBytes).toBe(2048);
    expect(db.trainings[0].files).toHaveLength(1);
  });

  it('rejects unsupported file types before storing anything', async () => {
    await expect(
      settle(
        repository.uploadFiles({
          organizationId: ORG,
          categoryId: 'cat-1',
          trainingId: 'trn-1',
          uploadedByUserId: 'user-1',
          files: [file('slides.pptx')],
        }),
      ),
    ).rejects.toBeInstanceOf(ApiFailure);
    expect(db.trainings[0].files).toHaveLength(0);
  });
});

const sampleFile = {
  id: 'f-1',
  fileName: 'a.pdf',
  contentType: 'application/pdf',
  sizeBytes: 10,
  uploadedByUserId: 'user-1',
  createdAt: '2026-01-01T00:00:00Z',
};
