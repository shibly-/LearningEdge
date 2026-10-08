import { UserRole } from '../core/models/user-role';

export interface NavItem {
  readonly label: string;
  readonly route: string;
  readonly roles: readonly UserRole[];
}

/**
 * Sidebar matrix. These roles mirror app.routes.ts — the guards remain the
 * access boundary; hiding a link is presentation only.
 */
export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Platform overview', route: '/platform', roles: [UserRole.SysAdmin] },
  { label: 'Organizations', route: '/platform/organizations', roles: [UserRole.SysAdmin] },
  { label: 'Organization admins', route: '/platform/org-admins', roles: [UserRole.SysAdmin] },
  { label: 'Users & staff', route: '/platform/users', roles: [UserRole.SysAdmin] },

  { label: 'Dashboard', route: '/dashboard/admin', roles: [UserRole.OrgAdmin] },
  { label: 'Dashboard', route: '/dashboard/staff', roles: [UserRole.Instructor] },

  { label: 'Users & staff', route: '/users', roles: [UserRole.OrgAdmin] },
  { label: 'Categories', route: '/categories', roles: [UserRole.OrgAdmin, UserRole.Instructor] },
  { label: 'Trainings', route: '/trainings', roles: [UserRole.OrgAdmin, UserRole.Instructor] },
  {
    label: 'Material processing',
    route: '/processing',
    roles: [UserRole.OrgAdmin, UserRole.Instructor],
  },
  { label: 'Trainee operations', route: '/staff-ops', roles: [UserRole.Instructor] },

  { label: 'My training', route: '/portal', roles: [UserRole.Learner] },
];

export function navItemsFor(role: UserRole | null): readonly NavItem[] {
  if (role === null) {
    return [];
  }
  return NAV_ITEMS.filter((item) => item.roles.includes(role));
}
