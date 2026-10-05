import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth/auth.service';
import { mockCredentials } from '../../core/auth/mock-users';
import { homeRouteFor, roleLabel } from '../../core/models/user-role';
import { describeError } from '../../core/stores/async-collection.store';
import { SpinnerComponent } from '../../shared/components/spinner.component';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, SpinnerComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly submitting = signal(false);
  private readonly failure = signal<string | null>(null);

  protected readonly isSubmitting = this.submitting.asReadonly();
  protected readonly errorMessage = this.failure.asReadonly();
  protected readonly showMockHints = !environment.production;

  protected readonly form = this.fb.group({
    username: this.fb.control('', [Validators.required]),
    password: this.fb.control('', [Validators.required]),
  });

  /** Dev affordance so each role is reachable without guessing. */
  protected readonly hints = mockCredentials().map((credential) => ({
    username: credential.username,
    role: roleLabel(credential.role),
	password: credential.password
  }));

  protected submit(): void {
    this.failure.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.auth
      .login(this.form.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (user) => {
          this.submitting.set(false);
          void this.router.navigateByUrl(this.returnUrl() ?? homeRouteFor(user.role));
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.failure.set(describeError(error));
        },
      });
  }

  protected fillHint(hint: any): void {
    //this.form.patchValue({ hint.username });
	this.form.patchValue({
      username: hint.username,
      password: hint.password
    });
  }

  private returnUrl(): string | null {
    const value = new URLSearchParams(window.location.search).get('returnUrl');
    // Only accept in-app paths, never an absolute URL.
    return value !== null && value.startsWith('/') && !value.startsWith('//') ? value : null;
  }
}
