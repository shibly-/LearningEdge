import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserRole, roleLabel } from '../../core/models/user-role';
import type { CreateUserCommand } from '../../core/models/user';
import { SpinnerComponent } from '../../shared/components/spinner.component';

/**
 * Mirrors the server-side guards on User: FirstName and Email throw
 * ArgumentNullException when null; LastName is optional.
 */
@Component({
  selector: 'app-user-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SpinnerComponent],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div class="row">
        <div
          class="le-field"
          [class.is-invalid]="form.controls.firstName.touched && form.controls.firstName.invalid"
        >
          <label for="user-first">First name</label>
          <input id="user-first" type="text" formControlName="firstName" />
          @if (form.controls.firstName.touched && form.controls.firstName.hasError('required')) {
            <span class="le-error">First name is required.</span>
          }
        </div>

        <div class="le-field">
          <label for="user-last">Last name</label>
          <input id="user-last" type="text" formControlName="lastName" />
          <span class="le-hint">Optional.</span>
        </div>
      </div>

      <div
        class="le-field"
        [class.is-invalid]="form.controls.email.touched && form.controls.email.invalid"
      >
        <label for="user-email">Email</label>
        <input id="user-email" type="email" formControlName="email" />
        @if (form.controls.email.touched && form.controls.email.hasError('required')) {
          <span class="le-error">Email is required.</span>
        }
        @if (form.controls.email.touched && form.controls.email.hasError('email')) {
          <span class="le-error">Enter a valid email address.</span>
        }
      </div>

      <div class="le-field">
        <label for="user-role">Role</label>
        <select id="user-role" formControlName="role">
          @for (option of roleOptions; track option.value) {
            <option [value]="option.value">{{ option.label }}</option>
          }
        </select>
      </div>

      <div class="actions">
        <button type="button" class="le-btn le-btn-secondary" (click)="cancel.emit()">
          Cancel
        </button>
        <button type="submit" class="le-btn" [disabled]="saving()">
          @if (saving()) {
            <app-spinner [size]="14" label="Saving" />
            <span>Saving…</span>
          } @else {
            <span>Add user</span>
          }
        </button>
      </div>
    </form>
  `,
  styles: `
    .row {
      display: grid;
      gap: 14px;
      grid-template-columns: 1fr 1fr;
    }

    .actions {
      display: flex;
      gap: 8px;
      justify-content: flex-end;
    }

    @media (max-width: 560px) {
      .row {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class UserFormComponent {
  readonly organizationId = input.required<string>();
  readonly saving = input(false);
  readonly save = output<CreateUserCommand>();
  readonly cancel = output<void>();

  private readonly fb = inject(NonNullableFormBuilder);

  /** OrgAdmin may create staff and trainees, not platform admins. */
  protected readonly roleOptions = [UserRole.Learner, UserRole.Instructor, UserRole.OrgAdmin].map(
    (value) => ({ value, label: roleLabel(value) }),
  );

  protected readonly form = this.fb.group({
    firstName: this.fb.control('', [Validators.required]),
    lastName: this.fb.control(''),
    email: this.fb.control('', [Validators.required, Validators.email]),
    role: this.fb.control(UserRole.Learner, [Validators.required]),
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.saving()) {
      return;
    }

    const value = this.form.getRawValue();
    this.save.emit({
      firstName: value.firstName,
      lastName: value.lastName,
      email: value.email,
      // A <select> yields a string even when the option value is numeric.
      role: Number(value.role) as UserRole,
      organizationId: this.organizationId(),
    });
  }
}
