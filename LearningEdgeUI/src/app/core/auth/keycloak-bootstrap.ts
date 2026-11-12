import { Injectable, effect, inject, untracked } from '@angular/core';
import { Router } from '@angular/router';
import {
  AutoRefreshTokenService,
  KEYCLOAK_EVENT_SIGNAL,
  KeycloakEventType,
} from 'keycloak-angular';
import Keycloak from 'keycloak-js';
import { environment } from '../../../environments/environment';
import { ToastService } from '../services/toast.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class KeycloakBootstrap {
  private readonly keycloak = inject(Keycloak);
  private readonly autoRefresh = inject(AutoRefreshTokenService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly events = inject(KEYCLOAK_EVENT_SIGNAL);

  constructor() {
    effect(() => {
      const type = this.events().type;
      if (type === KeycloakEventType.AuthLogout || type === KeycloakEventType.AuthRefreshError) {
        untracked(() => this.endSession());
      }
    });
  }

  /**
   * `provideKeycloak` is registered without `initOptions`, so keycloak-js does
   * not initialize itself. Angular runs app initializers concurrently and the
   * identity lookup needs a bearer token, so the two steps are ordered here.
   *
   * Both finish before the first route renders, which is what lets authGuard
   * and tenantGuard read the identity synchronously during navigation.
   */
  async initialize(): Promise<void> {
    this.autoRefresh.start({
      sessionTimeout: environment.keycloak.sessionTimeoutMs,
      onInactivityTimeout: 'logout',
    });

    const authenticated = await this.keycloak
      .init({
        // Resolves an existing session in a hidden iframe. keycloak-js falls
        // back to a full redirect where third-party cookies are blocked.
        onLoad: 'check-sso',
        silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
        scope: 'openid profile email',
      })
      .catch((error: unknown) => {
        this.toast.error(
          'Cannot reach the sign-in service. Check that Keycloak is running and that its certificate is trusted by this browser.',
        );
        console.warn('Keycloak initialization failed.', error);
        return false;
      });

    if (!authenticated) {
      return;
    }

    await this.auth.restore();
    const problem = this.auth.identityError();
    if (problem !== null) {
      this.toast.error(problem);
    }
  }

  private endSession(): void {
    if (!this.auth.isAuthenticated()) {
      return;
    }
    this.auth.clear();
    this.toast.info('Your session has ended. Please sign in again.');
    void this.router.navigate(['/login']);
  }
}
