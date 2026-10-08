import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import { apiPaths } from '../http/api-paths';
import type { UserDto } from '../models/user';
import { UserRole } from '../models/user-role';
import { MockDb } from './mock/mock-db';
import { HttpUserRepository, MockUserRepository } from './user.repository';

function user(
  id: string,
  firstName: string,
  lastName: string,
  role: UserRole,
  organizationId: string,
): UserDto {
  return { id, firstName, lastName, email: `${id}@test.example`, role, organizationId };
}

describe('apiPaths.user.list', () => {
  it('omits the query string without filters', () => {
    expect(apiPaths.user.list()).toMatch(/\/api\/v\d+\/user$/);
  });

  it('repeats role and encodes the organization id', () => {
    expect(
      apiPaths.user.list({ organizationId: 'a b', roles: [UserRole.Instructor, UserRole.Learner] }),
    ).toMatch(/\/user\?organizationId=a%20b&role=2&role=1$/);
  });
});

describe('HttpUserRepository.listAll', () => {
  let repository: HttpUserRepository;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [HttpUserRepository, provideHttpClient(), provideHttpClientTesting()],
    });
    repository = TestBed.inject(HttpUserRepository);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('sends the filters as query parameters', async () => {
    const admins = [user('u1', 'Ada', 'Lovelace', UserRole.OrgAdmin, 'org-1')];
    const result = firstValueFrom(repository.listAll({ roles: [UserRole.OrgAdmin] }));

    const request = http.expectOne(apiPaths.user.list({ roles: [UserRole.OrgAdmin] }));
    expect(request.request.method).toBe('GET');
    request.flush(admins);

    expect(await result).toEqual(admins);
  });
});

describe('MockUserRepository.listAll', () => {
  let repository: MockUserRepository;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({ providers: [MockUserRepository] });
    repository = TestBed.inject(MockUserRepository);
    const db = TestBed.inject(MockDb);
    db.organizations = [
      { id: 'org-1', name: 'Acme', description: '' },
      { id: 'org-2', name: 'Contoso', description: '' },
    ];
    db.users = [
      user('grace', 'Grace', 'Hopper', UserRole.Instructor, 'org-1'),
      user('ada', 'Ada', 'Lovelace', UserRole.OrgAdmin, 'org-1'),
      user('alan', 'Alan', 'Hopper', UserRole.Learner, 'org-1'),
      user('tim', 'Tim', 'Zed', UserRole.OrgAdmin, 'org-2'),
    ];
  });

  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  async function settle<T>(source: Observable<T>): Promise<T> {
    const result = firstValueFrom(source);
    result.catch(() => undefined);
    await vi.advanceTimersByTimeAsync(1000);
    return result;
  }

  it('returns every user ordered by last then first name without filters', async () => {
    const users = await settle(repository.listAll({}));

    expect(users.map((u) => u.id)).toEqual(['alan', 'grace', 'ada', 'tim']);
  });

  it('filters by role across organizations', async () => {
    const users = await settle(repository.listAll({ roles: [UserRole.OrgAdmin] }));

    expect(users.map((u) => u.id)).toEqual(['ada', 'tim']);
  });

  it('filters by organization and roles together', async () => {
    const users = await settle(
      repository.listAll({
        organizationId: 'org-1',
        roles: [UserRole.Instructor, UserRole.Learner],
      }),
    );

    expect(users.map((u) => u.id)).toEqual(['alan', 'grace']);
  });

  it('fails with not-found for an unknown organization', async () => {
    await expect(settle(repository.listAll({ organizationId: 'missing' }))).rejects.toBeInstanceOf(
      ApiFailure,
    );
  });
});
