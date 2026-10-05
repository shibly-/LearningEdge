import type { UserRole } from '../models/user-role';

export interface AuthUser {
  readonly id: string;
  readonly username: string;
  readonly displayName: string;
  readonly email: string;
  readonly role: UserRole;
  /** null for platform scope (SysAdmin); a GUID for every tenant-scoped role. */
  readonly organizationId: string | null;
  /** Accepted-and-ignored today: the API registers no authentication scheme. */
  readonly accessToken: string | null;
}

export interface Credentials {
  readonly username: string;
  readonly password: string;
}
