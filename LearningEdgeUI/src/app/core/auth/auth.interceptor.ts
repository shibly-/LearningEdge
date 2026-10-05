import { inject } from '@angular/core';
import type { HttpInterceptorFn } from '@angular/common/http';
import { SKIP_API_CONCERNS } from '../http/http-context';
import { OrganizationContextService } from '../services/organization-context.service';
import { AuthService } from './auth.service';

/**
 * Attaches the bearer token when one exists and the active tenant as
 * X-Organization-Id. The API ignores both today — it registers no
 * authentication scheme — but the UI contract is stable (spec 4.3).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.context.get(SKIP_API_CONCERNS)) {
    return next(req);
  }

  const token = inject(AuthService).accessToken();
  const organizationId = inject(OrganizationContextService).activeOrganizationId();

  const headers: Record<string, string> = {};
  if (token !== null) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  if (organizationId !== null) {
    headers['X-Organization-Id'] = organizationId;
  }

  if (Object.keys(headers).length === 0) {
    return next(req);
  }

  return next(req.clone({ setHeaders: headers }));
};
