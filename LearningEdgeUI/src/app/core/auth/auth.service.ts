import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { UserRole, isPlatformScoped } from '../models/user-role';
import { AUTH_PROVIDER } from './auth-provider';
import type { AuthUser, Credentials } from './auth-user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly provider = inject(AUTH_PROVIDER);

  private readonly user = signal<AuthUser | null>(null);

  readonly currentUser = this.user.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null);
  readonly role = computed<UserRole | null>(() => this.user()?.role ?? null);
  readonly accessToken = computed<string | null>(() => this.user()?.accessToken ?? null);
  readonly displayName = computed<string>(() => this.user()?.displayName ?? '');

  /** The organization the user belongs to. Not the same as the active one for SysAdmin. */
  readonly homeOrganizationId = computed<string | null>(() => this.user()?.organizationId ?? null);

  readonly isPlatformUser = computed<boolean>(() => {
    const role = this.role();
    return role !== null && isPlatformScoped(role);
  });

  constructor() {
    this.user.set(this.provider.restore());
  }

  login(credentials: Credentials): Observable<AuthUser> {
    return this.provider.login(credentials).pipe(tap((user) => this.user.set(user)));
  }

  logout(): Observable<void> {
    return this.provider.logout().pipe(tap(() => this.user.set(null)));
  }

  hasAnyRole(roles: readonly UserRole[]): boolean {
    const role = this.role();
    return role !== null && roles.includes(role);
  }
}
