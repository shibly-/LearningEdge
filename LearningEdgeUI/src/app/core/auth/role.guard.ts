import { inject } from '@angular/core';
import { Router } from '@angular/router';
import type { ActivatedRouteSnapshot, CanActivateFn } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { UserRole, homeRouteFor, isUserRole } from '../models/user-role';
import { AuthService } from './auth.service';

/** Attach to a route as `data: { roles: [UserRole.OrgAdmin] }`. */
export interface RoleRouteData {
  readonly roles: readonly UserRole[];
}

export function rolesOf(route: Pick<ActivatedRouteSnapshot, 'data'>): readonly UserRole[] {
  const raw: unknown = route.data['roles'];
  return Array.isArray(raw) ? raw.filter(isUserRole) : [];
}

/**
 * Redirects an authenticated-but-wrong-role user to their own landing route and
 * raises a toast. It must never leave the user on a blank screen.
 */
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);

  const role = auth.role();
  if (role === null) {
    return router.createUrlTree(['/login']);
  }

  const allowed = rolesOf(route);
  if (allowed.length === 0 || allowed.includes(role)) {
    return true;
  }

  toast.warning('You do not have access to that area.');
  return router.createUrlTree([homeRouteFor(role)]);
};
