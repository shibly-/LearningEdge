import { environment } from '../../../environments/environment';

/** Routes are URL-path versioned: /api/v1/... */
function base(): string {
  return `${environment.apiBaseUrl.replace(/\/+$/, '')}/api/${environment.apiVersion}`;
}

const seg = encodeURIComponent;

export const apiPaths = {
  organization: {
    list: () => `${base()}/organization`,
    create: () => `${base()}/organization`,
    byId: (id: string) => `${base()}/organization/${seg(id)}`,
    users: (id: string) => `${base()}/organization/${seg(id)}/users`,
  },
  user: {
    /** `role` repeats in the query string; the API matches any of them. */
    list: (filter: { organizationId?: string | null; roles?: readonly number[] } = {}) => {
      const query = [
        ...(filter.organizationId ? [`organizationId=${seg(filter.organizationId)}`] : []),
        ...(filter.roles ?? []).map((role) => `role=${role}`),
      ];
      return `${base()}/user${query.length > 0 ? `?${query.join('&')}` : ''}`;
    },
    create: () => `${base()}/user`,
    byId: (id: string) => `${base()}/user/${seg(id)}`,
  },
  category: {
    list: (organizationId: string) => `${base()}/organization/${seg(organizationId)}/category`,
    byId: (organizationId: string, id: string) =>
      `${base()}/organization/${seg(organizationId)}/category/${seg(id)}`,
  },
  training: {
    list: (categoryId: string) => `${base()}/category/${seg(categoryId)}/training`,
    byId: (categoryId: string, id: string) =>
      `${base()}/category/${seg(categoryId)}/training/${seg(id)}`,
    files: (categoryId: string, id: string) =>
      `${base()}/category/${seg(categoryId)}/training/${seg(id)}/files`,
    file: (categoryId: string, trainingId: string, fileId: string) =>
      `${base()}/category/${seg(categoryId)}/training/${seg(trainingId)}/files/${seg(fileId)}`,
  },
} as const;
