import { inject } from '@angular/core';
import { Router } from '@angular/router';
import type { CanActivateFn } from '@angular/router';
import { OrganizationContextService } from '../services/organization-context.service';
import { ToastService } from '../services/toast.service';
import { homeRouteFor } from '../models/user-role';
import { AuthService } from './auth.service';

/**
 * Rejects any route whose :organizationId does not match the active tenant.
 * SysAdmin is exempt. A tenant-scoped user must never reach another
 * organization's data by editing the URL (spec 1, spec 6).
 */
export const tenantGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const context = inject(OrganizationContextService);
  const router = inject(Router);
  const toast = inject(ToastService);

  const role = auth.role();
  if (role === null) {
    return router.createUrlTree(['/login']);
  }

  if (auth.isPlatformUser()) {
    return true;
  }

  const active = context.activeOrganizationId();
  if (active === null) {
    toast.error('No organization is associated with your account.');
    return router.createUrlTree(['/login']);
  }

  // Walk the matched segments so the check also covers nested child routes.
  const requested = findOrganizationParam(route);
  if (requested !== null && requested !== active) {
    toast.warning('That organization is outside your access.');
    return router.createUrlTree([homeRouteFor(role)]);
  }

  return true;
};

function findOrganizationParam(route: {
  paramMap: { get(name: string): string | null };
  parent: unknown;
}): string | null {
  let current: typeof route | null = route;
  while (current !== null) {
    const value = current.paramMap.get('organizationId');
    if (value !== null) {
      return value;
    }
    current = current.parent as typeof route | null;
  }
  return null;
}
