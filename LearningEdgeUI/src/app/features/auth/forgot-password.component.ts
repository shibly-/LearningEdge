import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

/**
 * MOCK: no backend endpoint. Password reset belongs to the identity provider,
 * which is not stood up yet (spec 3.9).
 */
@Component({
  selector: 'app-forgot-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <div class="auth-card le-card">
        @if (submitted()) {
          <h1>Check your inbox</h1>
          <p class="subtitle">
            If an account exists for that address, a reset link is on its way. The link expires in
            30 minutes.
          </p>
          <a class="le-btn" routerLink="/login">Back to sign in</a>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <h1>Reset password</h1>
            <p class="subtitle">Enter the email address associated with your account.</p>

            <div
              class="le-field"
              [class.is-invalid]="form.controls.email.touched && form.controls.email.invalid"
            >
              <label for="reset-email">Email</label>
              <input id="reset-email" type="email" autocomplete="email" formControlName="email" />
              @if (form.controls.email.touched && form.controls.email.hasError('required')) {
                <span class="le-error">Email is required.</span>
              }
              @if (form.controls.email.touched && form.controls.email.hasError('email')) {
                <span class="le-error">Enter a valid email address.</span>
              }
            </div>

            <button type="submit" class="le-btn submit">Send reset link</button>
            <p class="links"><a routerLink="/login">Back to sign in</a></p>
          </form>
        }
      </div>
    </div>
  `,
  styleUrl: './login.component.scss',
})
export class ForgotPasswordComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  private readonly done = signal(false);
  protected readonly submitted = this.done.asReadonly();

  protected readonly form = this.fb.group({
    email: this.fb.control('', [Validators.required, Validators.email]),
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    // Always report success so the form cannot be used to enumerate accounts.
    this.done.set(true);
  }
}
