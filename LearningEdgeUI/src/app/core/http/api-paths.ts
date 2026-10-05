import { environment } from '../../../environments/environment';

/**
 * Routes are URL-path versioned and the controller segment is SINGULAR and
 * lowercase: /api/v1/user, /api/v1/organization. Never /api/users.
 *
 * Only these four operations exist. There are no list, update, or delete
 * endpoints anywhere in the solution (spec 3.4).
 */
function base(): string {
  return `${environment.apiBaseUrl.replace(/\/+$/, '')}/api/${environment.apiVersion}`;
}

export const apiPaths = {
  user: {
    create: () => `${base()}/user`,
    byId: (id: string) => `${base()}/user/${encodeURIComponent(id)}`,
  },
  organization: {
    create: () => `${base()}/organization`,
    byId: (id: string) => `${base()}/organization/${encodeURIComponent(id)}`,
  },
} as const;
