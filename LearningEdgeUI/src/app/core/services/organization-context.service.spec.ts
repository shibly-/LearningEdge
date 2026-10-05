import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { UserRole } from '../models/user-role';
import type { AuthUser } from '../auth/auth-user';
import { AuthService } from '../auth/auth.service';
import { OrganizationContextService } from './organization-context.service';
import { TenantResetBus } from './tenant-reset-bus';

const ORG_A = '11111111-1111-1111-1111-111111111111';
const ORG_B = '22222222-2222-2222-2222-222222222222';

function userWith(role: UserRole, organizationId: string | null): AuthUser {
  return {
    id: 'user-1',
    username: 'tester',
    displayName: 'Tester',
    email: 'tester@example.com',
    role,
    organizationId,
    accessToken: null,
  };
}

function setup(role: UserRole, organizationId: string | null) {
  const currentUser = signal<AuthUser | null>(userWith(role, organizationId));

  TestBed.configureTestingModule({
    providers: [
      {
        provide: AuthService,
        useValue: {
          currentUser,
          isPlatformUser: () => currentUser()?.role === UserRole.SysAdmin,
        },
      },
    ],
  });

  const context = TestBed.inject(OrganizationContextService);
  const bus = TestBed.inject(TenantResetBus);

  // Deliberately no TestBed.tick(): tenant.guard.ts reads the active id during
  // navigation, which runs before effects flush. Ticking here would hide a
  // regression that bounces every tenant user straight back to /login.
  return { context, bus, currentUser };
}

describe('OrganizationContextService', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('pins a tenant-scoped user to their own organization', () => {
    const { context } = setup(UserRole.OrgAdmin, ORG_A);

    expect(context.activeOrganizationId()).toBe(ORG_A);
    expect(context.canSwitchOrganization()).toBe(false);
  });

  /** Regression: an effect-assigned tenant was still null when tenantGuard ran. */
  it('exposes the tenant as soon as the user signs in, before any tick', () => {
    const currentUser = signal<AuthUser | null>(null);
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            currentUser,
            isPlatformUser: () => currentUser()?.role === UserRole.SysAdmin,
          },
        },
      ],
    });
    const context = TestBed.inject(OrganizationContextService);
    expect(context.activeOrganizationId()).toBeNull();

    currentUser.set(userWith(UserRole.OrgAdmin, ORG_A));

    expect(context.activeOrganizationId()).toBe(ORG_A);
    expect(context.hasOrganization()).toBe(true);
  });

  it('ignores a switch attempt from a tenant-scoped user', () => {
    const { context } = setup(UserRole.Instructor, ORG_A);

    context.switchOrganization(ORG_B);

    expect(context.activeOrganizationId()).toBe(ORG_A);
  });

  it('lets SysAdmin switch organization', () => {
    const { context } = setup(UserRole.SysAdmin, null);

    context.switchOrganization(ORG_B);

    expect(context.canSwitchOrganization()).toBe(true);
    expect(context.activeOrganizationId()).toBe(ORG_B);
  });

  /** No cross-tenant data may survive a switch. */
  it('clears every registered store when the organization changes', () => {
    const { context, bus } = setup(UserRole.SysAdmin, null);

    let resets = 0;
    bus.register(() => resets++);

    context.switchOrganization(ORG_A);
    expect(resets).toBe(1);

    context.switchOrganization(ORG_B);
    expect(resets).toBe(2);
  });

  it('does not reset when switching to the organization already active', () => {
    const { context, bus } = setup(UserRole.SysAdmin, null);

    context.switchOrganization(ORG_A);
    let resets = 0;
    bus.register(() => resets++);

    context.switchOrganization(ORG_A);

    expect(resets).toBe(0);
  });

  it('clears the context on sign-out', () => {
    const { context, currentUser } = setup(UserRole.OrgAdmin, ORG_A);
    expect(context.activeOrganizationId()).toBe(ORG_A);

    currentUser.set(null);

    expect(context.activeOrganizationId()).toBeNull();
  });

  /** A switcher choice must not survive into the next session. */
  it('drops a SysAdmin selection when a different user signs in', () => {
    const { context, currentUser } = setup(UserRole.SysAdmin, null);
    context.switchOrganization(ORG_B);
    expect(context.activeOrganizationId()).toBe(ORG_B);

    currentUser.set({ ...userWith(UserRole.SysAdmin, null), id: 'user-2' });

    expect(context.activeOrganizationId()).toBeNull();
  });

  it('throws rather than querying with no tenant selected', () => {
    const { context } = setup(UserRole.SysAdmin, null);

    expect(() => context.requireOrganizationId()).toThrowError(/No active organization/);
  });
});
