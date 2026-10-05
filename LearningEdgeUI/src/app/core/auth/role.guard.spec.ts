import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { UserRole } from '../models/user-role';
import { ToastService } from '../services/toast.service';
import { roleGuard } from './role.guard';
import { AuthService } from './auth.service';

const warnings: string[] = [];

function configure(role: UserRole | null): void {
  warnings.length = 0;
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      { provide: AuthService, useValue: { role: () => role } },
      {
        provide: ToastService,
        useValue: { warning: (message: string) => warnings.push(message) },
      },
    ],
  });
}

function routeWithRoles(roles: readonly UserRole[] | undefined): ActivatedRouteSnapshot {
  return { data: roles === undefined ? {} : { roles } } as unknown as ActivatedRouteSnapshot;
}

const anyState = {} as RouterStateSnapshot;

describe('roleGuard', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('allows a role listed in route data', () => {
    configure(UserRole.OrgAdmin);
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeWithRoles([UserRole.OrgAdmin, UserRole.Instructor]), anyState),
    );
    expect(result).toBe(true);
  });

  it('allows any role when the route declares none', () => {
    configure(UserRole.Learner);
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeWithRoles(undefined), anyState),
    );
    expect(result).toBe(true);
  });

  // Never leave the user on a blank screen: redirect to their own landing route.
  it('redirects a wrong-role user to their own dashboard and warns them', () => {
    configure(UserRole.Learner);
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeWithRoles([UserRole.OrgAdmin]), anyState),
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/portal');
    expect(warnings).toEqual(['You do not have access to that area.']);
  });

  it('sends SysAdmin to the platform route when they hit a tenant-only area', () => {
    configure(UserRole.SysAdmin);
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeWithRoles([UserRole.Instructor]), anyState),
    );
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/platform');
  });

  it('redirects to /login when there is no role at all', () => {
    configure(null);
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeWithRoles([UserRole.OrgAdmin]), anyState),
    );
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });
});
