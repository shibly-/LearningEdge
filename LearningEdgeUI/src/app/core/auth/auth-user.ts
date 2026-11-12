import type { UserRole } from '../models/user-role';

export interface AuthUser {
  /**
   * The LearningEdge user id, recorded as the actor on writes. Falls back to
   * the Keycloak subject for a platform operator with no user record yet.
   */
  readonly id: string;
  readonly username: string;
  readonly displayName: string;
  readonly email: string;
  readonly role: UserRole;
  /** null for platform scope (SysAdmin); a GUID for every tenant-scoped role. */
  readonly organizationId: string | null;
  /** false when no API user record backs this identity. */
  readonly isProvisioned: boolean;
}
