import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { UserRole, isPlatformScoped } from '../models/user-role';
import { describeError } from '../stores/async-collection.store';
import { AUTH_PROVIDER } from './auth-provider';
import type { AuthUser } from './auth-user';

/**
 * Holds the signed-in identity. Nothing here runs at construction time: the
 * identity is resolved by KeycloakBootstrap during app initialization, which
 * keeps this service out of the HttpClient dependency cycle the auth
 * interceptor would otherwise create.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly provider = inject(AUTH_PROVIDER);

  private readonly user = signal<AuthUser | null>(null);
  private readonly failure = signal<string | null>(null);

  readonly currentUser = this.user.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null);
  readonly role = computed<UserRole | null>(() => this.user()?.role ?? null);
  readonly displayName = computed<string>(() => this.user()?.displayName ?? '');

  /**
   * Set when Keycloak authenticated the visitor but no LearningEdge identity
   * could be resolved for them. Signing in again would just repeat it, so the
   * login page offers to end the Keycloak session instead.
   */
  readonly identityError = this.failure.asReadonly();

  /** The organization the user belongs to. Not the same as the active one for SysAdmin. */
  readonly homeOrganizationId = computed<string | null>(() => this.user()?.organizationId ?? null);

  readonly isPlatformUser = computed<boolean>(() => {
    const role = this.role();
    return role !== null && isPlatformScoped(role);
  });

  /**
   * Resolves the identity behind an existing session. Awaited during bootstrap
   * so guards and tenant scoping can read it synchronously from then on.
   */
  async restore(): Promise<void> {
    try {
      this.user.set(await firstValueFrom(this.provider.restore()));
      this.failure.set(null);
    } catch (error: unknown) {
      this.user.set(null);
      this.failure.set(describeError(error));
    }
  }

  signIn(returnUrl: string | null): Promise<void> {
    return this.provider.login(returnUrl);
  }

  signOut(): Promise<void> {
    this.clear();
    return this.provider.logout();
  }

  /** Drops the local identity when the provider reports the session has ended. */
  clear(): void {
    this.user.set(null);
    this.failure.set(null);
  }

  /** A method, not a signal: the interceptor reads it once per request. */
  accessToken(): string | null {
    return this.provider.token();
  }

  hasAnyRole(roles: readonly UserRole[]): boolean {
    const role = this.role();
    return role !== null && roles.includes(role);
  }
}
