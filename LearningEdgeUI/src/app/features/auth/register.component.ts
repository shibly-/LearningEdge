import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
  type AbstractControl,
  type ValidationErrors,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import type { OrganizationChoice } from '../../core/models/organization';
import { OrganizationRepository } from '../../core/services/organization.repository';
import { ToastService } from '../../core/services/toast.service';
import { UserRepository } from '../../core/services/user.repository';
import { describeError } from '../../core/stores/async-collection.store';

/**
 * Public trainee registration. The account is stored as pending and is not
 * created in Keycloak until an OrgAdmin or SysAdmin approves it.
 */
@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './login.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly organizationsRepo = inject(OrganizationRepository);
  private readonly users = inject(UserRepository);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly organizations = signal<readonly OrganizationChoice[]>([]);
  protected readonly orgStatus = signal<'loading' | 'ready' | 'error'>('loading');
  protected readonly submitting = signal(false);
  protected readonly confirmation = signal<{ email: string; organizationName: string } | null>(
    null,
  );

  protected readonly form = this.fb.group(
    {
      firstName: this.fb.control('', [Validators.required, Validators.maxLength(50)]),
      lastName: this.fb.control('', [Validators.maxLength(50)]),
      email: this.fb.control('', [Validators.required, Validators.email, Validators.maxLength(100)]),
      organizationId: this.fb.control('', [Validators.required]),
      password: this.fb.control('', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(128),
      ]),
      confirmPassword: this.fb.control('', [Validators.required]),
    },
    { validators: passwordsMatch },
  );

  constructor() {
    this.loadOrganizations();
  }

  protected loadOrganizations(): void {
    this.orgStatus.set('loading');
    this.organizationsRepo
      .directory()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (organizations) => {
          this.organizations.set([...organizations].sort((a, b) => a.name.localeCompare(b.name)));
          this.orgStatus.set('ready');
        },
        error: (error: unknown) => {
          this.orgStatus.set('error');
          this.toast.error(describeError(error));
        },
      });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.submitting()) {
      return;
    }

    const value = this.form.getRawValue();
    const organizationName =
      this.organizations().find((org) => org.id === value.organizationId)?.name ??
      'your organization';
    this.submitting.set(true);

    this.users
      .register({
        firstName: value.firstName,
        lastName: value.lastName,
        email: value.email,
        password: value.password,
        organizationId: value.organizationId,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.confirmation.set({ email: value.email, organizationName });
          this.form.reset();
          this.toast.success(
            'Registration submitted. An administrator must approve it before you can sign in.',
          );
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.toast.error(describeError(error));
        },
      });
  }
}

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return password === confirm ? null : { passwordMismatch: true };
}
