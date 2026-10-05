/**
 * Mirrors LearningEdge.Domain.Common.Enums.UserRole.
 *
 * The API serializes this as a NUMBER. The [EnumMember] attributes on the C# enum
 * are a DataContractSerializer feature that System.Text.Json ignores, and no
 * JsonStringEnumConverter is registered, so the wire format is `"role": 3`.
 */
export enum UserRole {
  Learner = 1,
  Instructor = 2,
  OrgAdmin = 3,
  SysAdmin = 4,
}

export const ALL_USER_ROLES: readonly UserRole[] = [
  UserRole.SysAdmin,
  UserRole.OrgAdmin,
  UserRole.Instructor,
  UserRole.Learner,
];

const ROLE_LABELS: Readonly<Record<UserRole, string>> = {
  [UserRole.Learner]: 'Trainee',
  [UserRole.Instructor]: 'Staff',
  [UserRole.OrgAdmin]: 'Admin',
  [UserRole.SysAdmin]: 'Super Admin',
};

export function roleLabel(role: UserRole): string {
  return ROLE_LABELS[role] ?? 'Unknown';
}

export function isUserRole(value: unknown): value is UserRole {
  return typeof value === 'number' && value >= 1 && value <= 4;
}

/** SysAdmin operates across all organizations; everyone else is tenant-bound. */
export function isPlatformScoped(role: UserRole): boolean {
  return role === UserRole.SysAdmin;
}

/** Landing route per role, used by auth.guard and the /dashboard redirect. */
export function homeRouteFor(role: UserRole): string {
  switch (role) {
    case UserRole.SysAdmin:
      return '/platform';
    case UserRole.OrgAdmin:
      return '/dashboard/admin';
    case UserRole.Instructor:
      return '/dashboard/staff';
    case UserRole.Learner:
      return '/portal';
  }
}
