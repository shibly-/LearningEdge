import { Injectable } from '@angular/core';
import type { AuthUser } from './auth-user';
import { isUserRole } from '../models/user-role';

const SESSION_KEY = 'le.tms.session';

/**
 * sessionStorage, not localStorage: the session must not outlive the browser tab.
 */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  read(): AuthUser | null {
    let raw: string | null = null;
    try {
      raw = sessionStorage.getItem(SESSION_KEY);
    } catch {
      return null;
    }
    if (raw === null) {
      return null;
    }
    try {
      return parseAuthUser(JSON.parse(raw));
    } catch {
      this.clear();
      return null;
    }
  }

  write(user: AuthUser): void {
    try {
      sessionStorage.setItem(SESSION_KEY, JSON.stringify(user));
    } catch {
      // Storage can be unavailable in private modes; the in-memory signal still works.
    }
  }

  clear(): void {
    try {
      sessionStorage.removeItem(SESSION_KEY);
    } catch {
      // Nothing to do.
    }
  }
}

function parseAuthUser(value: unknown): AuthUser | null {
  if (value === null || typeof value !== 'object') {
    return null;
  }
  const candidate = value as Record<string, unknown>;
  const id = candidate['id'];
  const username = candidate['username'];
  const role = candidate['role'];
  const organizationId = candidate['organizationId'];

  if (typeof id !== 'string' || typeof username !== 'string' || !isUserRole(role)) {
    return null;
  }
  if (organizationId !== null && typeof organizationId !== 'string') {
    return null;
  }

  return {
    id,
    username,
    displayName: typeof candidate['displayName'] === 'string' ? candidate['displayName'] : username,
    email: typeof candidate['email'] === 'string' ? candidate['email'] : '',
    role,
    organizationId,
    accessToken: typeof candidate['accessToken'] === 'string' ? candidate['accessToken'] : null,
  };
}
