import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { anonymousOnlyGuard, authGuard } from './auth.guard';
import { AuthService } from './auth.service';

function configure(isAuthenticated: boolean): void {
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      { provide: AuthService, useValue: { isAuthenticated: () => isAuthenticated } },
    ],
  });
}

const emptyRoute = {} as ActivatedRouteSnapshot;

function stateAt(url: string): RouterStateSnapshot {
  return { url } as RouterStateSnapshot;
}

describe('authGuard', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('allows an authenticated user through', () => {
    configure(true);
    const result = TestBed.runInInjectionContext(() => authGuard(emptyRoute, stateAt('/users')));
    expect(result).toBe(true);
  });

  it('redirects an anonymous visitor to /login and preserves the target', () => {
    configure(false);
    const result = TestBed.runInInjectionContext(() => authGuard(emptyRoute, stateAt('/users/42')));

    expect(result).toBeInstanceOf(UrlTree);
    const serialized = TestBed.inject(Router).serializeUrl(result as UrlTree);
    expect(serialized).toContain('/login');
    expect(serialized).toContain('returnUrl=%2Fusers%2F42');
  });
});

describe('anonymousOnlyGuard', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('allows an anonymous visitor to reach the login page', () => {
    configure(false);
    const result = TestBed.runInInjectionContext(() =>
      anonymousOnlyGuard(emptyRoute, stateAt('/login')),
    );
    expect(result).toBe(true);
  });

  it('bounces a signed-in user to the dashboard', () => {
    configure(true);
    const result = TestBed.runInInjectionContext(() =>
      anonymousOnlyGuard(emptyRoute, stateAt('/login')),
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/dashboard');
  });
});
