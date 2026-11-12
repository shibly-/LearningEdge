import { UserRole } from '../models/user-role';

/** The claims this app reads out of a Keycloak access token. */
export interface TokenIdentity {
  /** Keycloak's own user id. Not the LearningEdge user id. */
  readonly subject: string;
  readonly username: string;
  readonly displayName: string;
  readonly email: string;
  /** Highest realm role that maps onto UserRole, or null when none do. */
  readonly role: UserRole | null;
}

/**
 * Realm role names, lowercased so a realm that spells them differently still
 * resolves. Only used to recognise a platform operator who has no API user
 * record yet — otherwise the API record is authoritative for the role.
 */
const ROLE_BY_REALM_ROLE: Readonly<Record<string, UserRole>> = {
  sysadmin: UserRole.SysAdmin,
  orgadmin: UserRole.OrgAdmin,
  instructor: UserRole.Instructor,
  learner: UserRole.Learner,
};

/** Returns null when the token carries no subject, which makes it unusable. */
export function readTokenIdentity(token: unknown): TokenIdentity | null {
  const claims = asRecord(token);
  if (claims === null) {
    return null;
  }

  const subject = asString(claims['sub']);
  if (subject === null) {
    return null;
  }

  const email = asString(claims['email']) ?? '';
  const username = asString(claims['preferred_username']) ?? (email.length > 0 ? email : subject);

  return {
    subject,
    username,
    displayName: displayName(claims) ?? username,
    email,
    role: highestRole(realmRoles(claims)),
  };
}

function displayName(claims: Record<string, unknown>): string | null {
  const name = asString(claims['name']);
  if (name !== null) {
    return name;
  }
  const parts = [asString(claims['given_name']), asString(claims['family_name'])].filter(
    (part): part is string => part !== null,
  );
  return parts.length > 0 ? parts.join(' ') : null;
}

function realmRoles(claims: Record<string, unknown>): readonly string[] {
  const realmAccess = asRecord(claims['realm_access']);
  const roles = realmAccess?.['roles'];
  return Array.isArray(roles)
    ? roles.filter((role): role is string => typeof role === 'string')
    : [];
}

/** UserRole ascends with privilege, so the highest match wins. */
function highestRole(roles: readonly string[]): UserRole | null {
  const matched = roles
    .map((role) => ROLE_BY_REALM_ROLE[role.toLowerCase()])
    .filter((role): role is UserRole => role !== undefined);
  return matched.length > 0 ? Math.max(...matched) : null;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value !== null && typeof value === 'object' ? (value as Record<string, unknown>) : null;
}

function asString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value : null;
}
