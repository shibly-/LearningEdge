import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

/**
 * MOCK: no backend endpoint. Self-service registration is intentionally not
 * wired to POST /api/v1/user — that endpoint requires an organizationId and
 * would let a visitor place themselves inside any tenant. Accounts are
 * provisioned by an administrator; this screen only records interest.
 */
@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <div class="auth-card le-card">
        @if (submitted()) {
          <h1>Request received</h1>
          <p class="subtitle">
            An administrator will review your request and provision an account for your
            organization.
          </p>
          <a class="le-btn" routerLink="/login">Back to sign in</a>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <h1>Request access</h1>
            <p class="subtitle">
              Accounts are created by your organization administrator. Send a request and they will
              be in touch.
            </p>

            <div
              class="le-field"
              [class.is-invalid]="form.controls.fullName.touched && form.controls.fullName.invalid"
            >
              <label for="fullName">Full name</label>
              <input id="fullName" type="text" autocomplete="name" formControlName="fullName" />
              @if (form.controls.fullName.touched && form.controls.fullName.hasError('required')) {
                <span class="le-error">Full name is required.</span>
              }
            </div>

            <div
              class="le-field"
              [class.is-invalid]="form.controls.email.touched && form.controls.email.invalid"
            >
              <label for="email">Work email</label>
              <input id="email" type="email" autocomplete="email" formControlName="email" />
              @if (form.controls.email.touched && form.controls.email.hasError('required')) {
                <span class="le-error">Email is required.</span>
              }
              @if (form.controls.email.touched && form.controls.email.hasError('email')) {
                <span class="le-error">Enter a valid email address.</span>
              }
            </div>

            <div
              class="le-field"
              [class.is-invalid]="
                form.controls.organization.touched && form.controls.organization.invalid
              "
            >
              <label for="organization">Organization</label>
              <input id="organization" type="text" formControlName="organization" />
              @if (
                form.controls.organization.touched &&
                form.controls.organization.hasError('required')
              ) {
                <span class="le-error">Organization is required.</span>
              }
            </div>

            <button type="submit" class="le-btn submit">Send request</button>
            <p class="links"><a routerLink="/login">Back to sign in</a></p>
          </form>
        }
      </div>
    </div>
  `,
  styleUrl: './login.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  private readonly done = signal(false);
  protected readonly submitted = this.done.asReadonly();

  protected readonly form = this.fb.group({
    fullName: this.fb.control('', [Validators.required]),
    email: this.fb.control('', [Validators.required, Validators.email]),
    organization: this.fb.control('', [Validators.required]),
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.done.set(true);
  }
}
