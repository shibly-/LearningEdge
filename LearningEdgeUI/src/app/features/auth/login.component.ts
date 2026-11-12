import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { SpinnerComponent } from '../../shared/components/spinner.component';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, SpinnerComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);

  private readonly redirecting = signal(false);

  protected readonly isRedirecting = this.redirecting.asReadonly();

  /**
   * Set when Keycloak authenticated the visitor but no LearningEdge identity
   * matched. Signing in again would repeat it, so the only way forward is to
   * end the Keycloak session and come back as somebody else.
   */
  protected readonly identityError = this.auth.identityError;

  protected signIn(): void {
    if (this.redirecting()) {
      return;
    }
    this.redirecting.set(true);
    // Leaves the page on success, so the flag is only reset on failure.
    void this.auth.signIn(this.returnUrl()).catch(() => this.redirecting.set(false));
  }

  protected signOut(): void {
    if (this.redirecting()) {
      return;
    }
    this.redirecting.set(true);
    void this.auth.signOut().catch(() => this.redirecting.set(false));
  }

  private returnUrl(): string | null {
    const value = new URLSearchParams(window.location.search).get('returnUrl');
    // Only accept in-app paths, never an absolute URL.
    return value !== null && value.startsWith('/') && !value.startsWith('//') ? value : null;
  }
}
