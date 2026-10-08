import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { USER_LIMITS } from '../../core/models/user';
import { UserRole } from '../../core/models/user-role';
import { UserRepository } from '../../core/services/user.repository';
import { ToastService } from '../../core/services/toast.service';
import { OrganizationStore } from '../../core/stores/organization.store';
import { describeError } from '../../core/stores/async-collection.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';

/**
 * Provisions an OrgAdmin into a chosen tenant. This uses the live
 * POST /api/v1/user endpoint, which returns only the new GUID (spec 3.3).
 */
@Component({
  selector: 'app-org-admin-creator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SpinnerComponent,
  ],
  template: `
    <app-page-header
      title="Add organization admin"
      subtitle="Provision an administrator for a tenant."
    >
      <a class="le-btn le-btn-secondary" routerLink="/platform/org-admins">Back to admins</a>
    </app-page-header>

    @if (organizations.isLoading()) {
      <div class="le-card" style="padding: 18px">
        <app-skeleton [count]="3" label="Loading organizations" />
      </div>
    } @else if (organizations.hasError()) {
      <app-error-state [message]="organizations.error() ?? ''" (retry)="reload()" />
    } @else if (organizations.isEmpty()) {
      <app-empty-state
        title="No organizations yet"
        message="Create an organization before provisioning its administrator."
      />
    } @else {
      <div class="le-card form-card">
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div
            class="le-field"
            [class.is-invalid]="
              form.controls.organizationId.touched && form.controls.organizationId.invalid
            "
          >
            <label for="admin-org">Organization</label>
            <select id="admin-org" formControlName="organizationId">
              <option value="">Select an organization…</option>
              @for (org of organizations.sortedByName(); track org.id) {
                <option [value]="org.id">{{ org.name }}</option>
              }
            </select>
            @if (
              form.controls.organizationId.touched &&
              form.controls.organizationId.hasError('required')
            ) {
              <span class="le-error">Select an organization.</span>
            }
          </div>

          <div class="row">
            <div
              class="le-field"
              [class.is-invalid]="
                form.controls.firstName.touched && form.controls.firstName.invalid
              "
            >
              <label for="admin-first">First name</label>
              <input id="admin-first" type="text" formControlName="firstName" maxlength="50" />
              @if (
                form.controls.firstName.touched && form.controls.firstName.hasError('required')
              ) {
                <span class="le-error">First name is required.</span>
              }
            </div>

            <div class="le-field">
              <label for="admin-last">Last name</label>
              <input id="admin-last" type="text" formControlName="lastName" maxlength="50" />
              <span class="le-hint">Optional server-side.</span>
            </div>
          </div>

          <div
            class="le-field"
            [class.is-invalid]="form.controls.email.touched && form.controls.email.invalid"
          >
            <label for="admin-email">Email</label>
            <input id="admin-email" type="email" formControlName="email" maxlength="100" />
            @if (form.controls.email.touched && form.controls.email.hasError('required')) {
              <span class="le-error">Email is required.</span>
            }
            @if (form.controls.email.touched && form.controls.email.hasError('email')) {
              <span class="le-error">Enter a valid email address.</span>
            }
          </div>

          <button type="submit" class="le-btn" [disabled]="isSaving()">
            @if (isSaving()) {
              <app-spinner [size]="14" label="Creating" />
              <span>Creating…</span>
            } @else {
              <span>Create administrator</span>
            }
          </button>
        </form>

        @if (lastCreatedId() !== null) {
          <p class="created">
            Created with id <code>{{ lastCreatedId() }}</code
            >. The API returns only the identifier, so re-fetch if you need the full record.
          </p>
        }
      </div>
    }
  `,
  styles: `
    .form-card {
      max-width: 640px;
      padding: 20px;
    }

    .row {
      display: grid;
      gap: 14px;
      grid-template-columns: 1fr 1fr;
    }

    .created {
      margin: 16px 0 0;
      padding: 10px 12px;
      border-radius: var(--le-radius);
      background: var(--le-success-bg);
      color: var(--le-success);
      font-size: 0.9rem;
    }

    @media (max-width: 560px) {
      .row {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class OrgAdminCreatorComponent {
  protected readonly organizations = inject(OrganizationStore);

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly users = inject(UserRepository);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly saving = signal(false);
  private readonly createdId = signal<string | null>(null);

  protected readonly isSaving = this.saving.asReadonly();
  protected readonly lastCreatedId = this.createdId.asReadonly();

  protected readonly form = this.fb.group({
    organizationId: this.fb.control('', [Validators.required]),
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
  });

  constructor() {
    if (this.organizations.status() === 'idle') {
      this.organizations.loadAll();
    }
  }

  protected reload(): void {
    this.organizations.loadAll();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.saving()) {
      return;
    }

    this.saving.set(true);
    const value = this.form.getRawValue();

    this.users
      .create({ ...value, role: UserRole.OrgAdmin })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (id) => {
          this.saving.set(false);
          this.createdId.set(id);
          this.toast.success(`Administrator created for the selected organization.`);
          this.form.reset({ organizationId: value.organizationId });
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.toast.error(describeError(error));
        },
      });
  }
}
