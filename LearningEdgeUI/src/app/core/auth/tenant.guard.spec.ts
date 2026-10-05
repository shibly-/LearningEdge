import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { UserRole } from '../models/user-role';
import { OrganizationContextService } from '../services/organization-context.service';
import { ToastService } from '../services/toast.service';
import { tenantGuard } from './tenant.guard';
import { AuthService } from './auth.service';

const ORG_A = '11111111-1111-1111-1111-111111111111';
const ORG_B = '22222222-2222-2222-2222-222222222222';

const messages: string[] = [];

function configure(role: UserRole, activeOrganizationId: string | null): void {
  messages.length = 0;
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      {
        provide: AuthService,
        useValue: { role: () => role, isPlatformUser: () => role === UserRole.SysAdmin },
      },
      {
        provide: OrganizationContextService,
        useValue: { activeOrganizationId: () => activeOrganizationId },
      },
      {
        provide: ToastService,
        useValue: {
          warning: (message: string) => messages.push(message),
          error: (message: string) => messages.push(message),
        },
      },
    ],
  });
}

/** Builds a snapshot chain so the guard's parent walk is exercised. */
function routeWithOrgParam(
  organizationId: string | null,
  depth: 'self' | 'parent' = 'self',
): ActivatedRouteSnapshot {
  const leaf = {
    paramMap: {
      get: (name: string) =>
        name === 'organizationId' && depth === 'self' ? organizationId : null,
    },
    parent: null as unknown,
  };

  if (depth === 'parent') {
    leaf.parent = {
      paramMap: { get: (name: string) => (name === 'organizationId' ? organizationId : null) },
      parent: null,
    };
  }

  return leaf as unknown as ActivatedRouteSnapshot;
}

const anyState = {} as RouterStateSnapshot;

describe('tenantGuard', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('allows a tenant user whose active organization matches the route', () => {
    configure(UserRole.OrgAdmin, ORG_A);
    const result = TestBed.runInInjectionContext(() =>
      tenantGuard(routeWithOrgParam(ORG_A), anyState),
    );
    expect(result).toBe(true);
  });

  it('allows routes that carry no organization parameter', () => {
    configure(UserRole.Instructor, ORG_A);
    const result = TestBed.runInInjectionContext(() =>
      tenantGuard(routeWithOrgParam(null), anyState),
    );
    expect(result).toBe(true);
  });

  // Editing the URL must not reach another tenant's data.
  it('blocks a tenant user pointed at a different organization', () => {
    configure(UserRole.OrgAdmin, ORG_A);
    const result = TestBed.runInInjectionContext(() =>
      tenantGuard(routeWithOrgParam(ORG_B), anyState),
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/dashboard/admin');
    expect(messages).toEqual(['That organization is outside your access.']);
  });

  it('also inspects parent route parameters', () => {
    configure(UserRole.OrgAdmin, ORG_A);
    const result = TestBed.runInInjectionContext(() =>
      tenantGuard(routeWithOrgParam(ORG_B, 'parent'), anyState),
    );
    expect(result).toBeInstanceOf(UrlTree);
  });

  it('exempts SysAdmin from tenant scoping', () => {
    configure(UserRole.SysAdmin, null);
    const result = TestBed.runInInjectionContext(() =>
      tenantGuard(routeWithOrgParam(ORG_B), anyState),
    );
    expect(result).toBe(true);
  });

  it('sends a tenant user with no organization back to login', () => {
    configure(UserRole.Learner, null);
    const result = TestBed.runInInjectionContext(() =>
      tenantGuard(routeWithOrgParam(null), anyState),
    );

    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
    expect(messages).toEqual(['No organization is associated with your account.']);
  });
});
