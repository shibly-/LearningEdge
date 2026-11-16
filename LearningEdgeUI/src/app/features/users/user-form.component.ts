import { ChangeDetectionStrategy, Component, OnInit, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserRole, roleLabel } from '../../core/models/user-role';
import { USER_LIMITS, type CreateUserCommand, type UserDto } from '../../core/models/user';
import { SpinnerComponent } from '../../shared/components/spinner.component';

/** Staff and trainees. Organization admins are appointed by a system admin. */
const TENANT_ROLES: readonly UserRole[] = [UserRole.Learner, UserRole.Instructor];

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
          <input
            id="user-first"
            type="text"
            formControlName="firstName"
            [attr.maxlength]="limits.firstName"
          />
          @if (form.controls.firstName.touched && form.controls.firstName.hasError('required')) {
            <span class="le-error">First name is required.</span>
          }
        </div>

        <div class="le-field">
          <label for="user-last">Last name</label>
          <input
            id="user-last"
            type="text"
            formControlName="lastName"
            [attr.maxlength]="limits.lastName"
          />
          <span class="le-hint">Optional.</span>
        </div>
      </div>

      <div
        class="le-field"
        [class.is-invalid]="form.controls.email.touched && form.controls.email.invalid"
      >
        <label for="user-email">Email</label>
        <input
          id="user-email"
          type="email"
          formControlName="email"
          [attr.maxlength]="limits.email"
        />
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
        @if (roleLocked) {
          <span class="le-hint">This role is managed by a system admin.</span>
        }
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
            <span>{{ initial() === null ? 'Add user' : 'Save changes' }}</span>
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
export class UserFormComponent implements OnInit {
  readonly organizationId = input.required<string>();
  readonly saving = input(false);
  /** Pre-fills the form for editing; null for a new user. */
  readonly initial = input<UserDto | null>(null);
  /**
   * Roles this form may assign. Defaults to staff and trainee. The system
   * admin page passes organization admin only.
   */
  readonly assignableRoles = input<readonly UserRole[] | null>(null);
  readonly save = output<CreateUserCommand>();
  readonly cancel = output<void>();

  protected readonly limits = USER_LIMITS;

  private readonly fb = inject(NonNullableFormBuilder);

  protected roleOptions: { value: UserRole; label: string }[] = TENANT_ROLES.map((value) => ({
    value,
    label: roleLabel(value),
  }));
  protected roleLocked = false;

  protected readonly form = this.fb.group({
    firstName: this.fb.control('', [
      Validators.required,
      Validators.maxLength(USER_LIMITS.firstName),
    ]),
    lastName: this.fb.control('', [Validators.maxLength(USER_LIMITS.lastName)]),
    email: this.fb.control('', [
      Validators.required,
      Validators.email,
      Validators.maxLength(USER_LIMITS.email),
    ]),
    role: this.fb.control(UserRole.Learner, [Validators.required]),
  });

  ngOnInit(): void {
    const allowed = this.assignableRoles() ?? TENANT_ROLES;
    this.roleOptions = allowed.map((value) => ({ value, label: roleLabel(value) }));
    this.form.controls.role.setValue(allowed[0] ?? UserRole.Learner);

    const initial = this.initial();
    if (initial === null) {
      return;
    }

    if (!allowed.includes(initial.role)) {
      this.roleLocked = true;
      this.roleOptions = [{ value: initial.role, label: roleLabel(initial.role) }];
    }

    this.form.reset({
      firstName: initial.firstName,
      lastName: initial.lastName,
      email: initial.email,
      role: initial.role,
    });
    if (this.roleLocked) {
      this.form.controls.role.disable();
    }
  }

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
