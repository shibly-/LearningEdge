import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import type { OrganizationDto } from '../models/organization';
import type { UserDto } from '../models/user';
import { UserRole } from '../models/user-role';
import { OrganizationRepository } from '../services/organization.repository';
import { UserRepository } from '../services/user.repository';
import { ApiIdentityResolver } from './api-identity.resolver';
import type { AuthUser } from './auth-user';

const ORG_A: OrganizationDto = { id: 'org-a', name: 'Northwind', description: '' };
const ORG_B: OrganizationDto = { id: 'org-b', name: 'Contoso', description: '' };

const admin: UserDto = {
  id: 'api-admin',
  firstName: 'Mark',
  lastName: 'Price',
  email: 'mark.price@simplelearn.com',
  role: UserRole.OrgAdmin,
  organizationId: ORG_A.id,
};

function login(overrides: Partial<AuthUser> = {}): AuthUser {
  return {
    id: 'user-a1',
    username: 'mark.price@simplelearn.com',
    displayName: 'Configured name',
    email: 'mark.price@simplelearn.com',
    role: UserRole.OrgAdmin,
    organizationId: 'dcd9946b-908d-459e-85c2-66b590c50ad2',
    accessToken: null,
    ...overrides,
  };
}

function setup(usersByOrg: Record<string, readonly UserDto[]>, orgs = [ORG_A, ORG_B]) {
  TestBed.configureTestingModule({
    providers: [
      { provide: OrganizationRepository, useValue: { list: () => of(orgs) } },
      {
        provide: UserRepository,
        useValue: { listByOrganization: (id: string) => of(usersByOrg[id] ?? []) },
      },
    ],
  });
  return TestBed.inject(ApiIdentityResolver);
}

describe('ApiIdentityResolver', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('swaps in the API record matched by email, ignoring case', async () => {
    const resolver = setup({ [ORG_A.id]: [admin] });

    const user = await firstValueFrom(resolver.resolve(login()));

    expect(user.id).toBe('api-admin');
    expect(user.organizationId).toBe(ORG_A.id);
    expect(user.displayName).toBe('Mark Price');
    expect(user.username).toBe('mark.price@simplelearn.com');
  });

  it('takes the role from the API record', async () => {
    const resolver = setup({ [ORG_A.id]: [{ ...admin, role: UserRole.Instructor }] });

    const user = await firstValueFrom(resolver.resolve(login()));

    expect(user.role).toBe(UserRole.Instructor);
  });

  it('keeps a matched SysAdmin at platform scope', async () => {
    const resolver = setup({
      [ORG_B.id]: [{ ...admin, role: UserRole.SysAdmin, organizationId: ORG_B.id }],
    });

    const user = await firstValueFrom(
      resolver.resolve(login({ role: UserRole.SysAdmin, organizationId: null })),
    );

    expect(user.id).toBe('api-admin');
    expect(user.organizationId).toBeNull();
  });

  it('lets an unmatched SysAdmin in so an empty database can be seeded', async () => {
    const resolver = setup({}, []);
    const sysAdmin = login({ role: UserRole.SysAdmin, organizationId: null, email: 'ops@x.io' });

    expect(await firstValueFrom(resolver.resolve(sysAdmin))).toEqual(sysAdmin);
  });

  it('rejects a tenant login with no API user', async () => {
    const resolver = setup({ [ORG_A.id]: [] });

    await expect(firstValueFrom(resolver.resolve(login()))).rejects.toMatchObject({
      kind: 'not-found',
    });
  });

  it('rejects an email that exists in more than one organization', async () => {
    const resolver = setup({
      [ORG_A.id]: [admin],
      [ORG_B.id]: [{ ...admin, id: 'other', organizationId: ORG_B.id }],
    });

    await expect(firstValueFrom(resolver.resolve(login()))).rejects.toMatchObject({
      kind: 'conflict',
    });
  });
});
