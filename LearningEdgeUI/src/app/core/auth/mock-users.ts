import { environment } from '../../../environments/environment';
import type { MockCredential } from '../../../environments/environment.model';
import { roleLabel } from '../models/user-role';
import type { AuthUser } from './auth-user';

/**
 * Dev-only login table. Credentials come from environment.development.ts, which
 * is gitignored — never inline a password here.
 *
 * Run `npm run setup:env` to create it from the committed example.
 */
export function mockCredentials(): readonly MockCredential[] {
  return environment.mockCredentials;
}

export function findMockUser(username: string, password: string): AuthUser | null {
  const match = environment.mockCredentials.find(
    (candidate) =>
      candidate.username.toLowerCase() === username.trim().toLowerCase() &&
      candidate.password === password,
  );
  return match ? toAuthUser(match) : null;
}

function toAuthUser(credential: MockCredential): AuthUser {
  return {
    id: credential.userId ?? deterministicId(credential.username),
    username: credential.username,
    displayName: credential.displayName ?? roleLabel(credential.role),
    email: credential.email ?? `${credential.username}@learningedge.local`,
    role: credential.role,
    organizationId: credential.organizationId,
    accessToken: null,
  };
}

/** Stable GUID-shaped id so mock sessions survive a reload with the same identity. */
function deterministicId(username: string): string {
  let hash = 0x811c9dc5;
  for (let i = 0; i < username.length; i++) {
    hash ^= username.charCodeAt(i);
    hash = Math.imul(hash, 0x01000193) >>> 0;
  }
  const hex = hash.toString(16).padStart(8, '0');
  return `${hex}-0000-4000-8000-${hex}0000`;
}
