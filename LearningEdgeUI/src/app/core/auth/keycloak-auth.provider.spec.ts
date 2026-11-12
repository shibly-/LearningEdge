import { TestBed } from '@angular/core/testing';
import Keycloak from 'keycloak-js';
import { firstValueFrom, of } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import type { UserDto } from '../models/user';
import { UserRole } from '../models/user-role';
import { UserRepository } from '../services/user.repository';
import { KeycloakAuthProvider } from './keycloak-auth.provider';

const ORG_ID = 'dcd9946b-908d-459e-85c2-66b590c50ad2';

function tokenParsed(roles: readonly string[]): Record<string, unknown> {
  return {
    sub: 'kc-5f2a',
    preferred_username: 'mark.price',
    name: 'From Token',
    email: 'mark.price@simplelearn.com',
    realm_access: { roles },
  };
}

function record(overrides: Partial<UserDto> = {}): UserDto {
  return {
    id: '7b1c6d8e-0000-4000-8000-000000000001',
    firstName: 'Mark',
    lastName: 'Price',
    email: 'mark.price@simplelearn.com',
    role: UserRole.OrgAdmin,
    organizationId: ORG_ID,
    ...overrides,
  };
}

function setup(options: {
  authenticated?: boolean;
  roles?: readonly string[];
  me?: UserDto | null;
}): KeycloakAuthProvider {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: Keycloak,
        useValue: {
          authenticated: options.authenticated ?? true,
          tokenParsed: tokenParsed(options.roles ?? ['OrgAdmin']),
          token: 'jwt-value',
        },
      },
      { provide: UserRepository, useValue: { me: () => of(options.me ?? null) } },
    ],
  });
  return TestBed.inject(KeycloakAuthProvider);
}

describe('KeycloakAuthProvider', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('reports nobody signed in when there is no Keycloak session', async () => {
    const provider = setup({ authenticated: false });
    await expect(firstValueFrom(provider.restore())).resolves.toBeNull();
  });

  it('takes id, role and organization from the API record, not the token', async () => {
    const provider = setup({ roles: ['Learner'], me: record() });

    await expect(firstValueFrom(provider.restore())).resolves.toEqual({
      id: '7b1c6d8e-0000-4000-8000-000000000001',
      username: 'mark.price',
      displayName: 'Mark Price',
      email: 'mark.price@simplelearn.com',
      role: UserRole.OrgAdmin,
      organizationId: ORG_ID,
      isProvisioned: true,
    });
  });

  it('clears the organization for a platform record, which is not tenant scoped', async () => {
    const provider = setup({ me: record({ role: UserRole.SysAdmin }) });

    const user = await firstValueFrom(provider.restore());
    expect(user?.organizationId).toBeNull();
  });

  it('admits a platform operator with no record yet, so an empty database can be seeded', async () => {
    const provider = setup({ roles: ['SysAdmin'], me: null });

    await expect(firstValueFrom(provider.restore())).resolves.toEqual({
      id: 'kc-5f2a',
      username: 'mark.price',
      displayName: 'From Token',
      email: 'mark.price@simplelearn.com',
      role: UserRole.SysAdmin,
      organizationId: null,
      isProvisioned: false,
    });
  });

  it('refuses a tenant user with no record rather than guessing a tenant', async () => {
    const provider = setup({ roles: ['Instructor'], me: null });

    await expect(firstValueFrom(provider.restore())).rejects.toThrow(
      /No LearningEdge user has the email mark\.price@simplelearn\.com/,
    );
    await expect(firstValueFrom(provider.restore())).rejects.toBeInstanceOf(ApiFailure);
  });

  it('rejects a token with no subject', async () => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: Keycloak,
          useValue: { authenticated: true, tokenParsed: { email: 'x@y.z' }, token: 'jwt' },
        },
        { provide: UserRepository, useValue: { me: () => of(null) } },
      ],
    });
    const provider = TestBed.inject(KeycloakAuthProvider);

    await expect(firstValueFrom(provider.restore())).rejects.toBeInstanceOf(ApiFailure);
  });

  it('reads the token live so a refresh is picked up without re-resolving identity', () => {
    const provider = setup({ me: record() });
    expect(provider.token()).toBe('jwt-value');
  });
});
