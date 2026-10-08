import { ChangeDetectionStrategy, Component, OnInit, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  ORGANIZATION_LIMITS,
  type CreateOrganizationCommand,
  type OrganizationDto,
} from '../../core/models/organization';
import { SpinnerComponent } from '../../shared/components/spinner.component';

/**
 * Organization has only Name and Description server-side. Do not add fields the
 * API will drop (spec 3.5). Used for both create and edit.
 */
@Component({
  selector: 'app-organization-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SpinnerComponent],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div
        class="le-field"
        [class.is-invalid]="form.controls.name.touched && form.controls.name.invalid"
      >
        <label for="org-name">Name</label>
        <input id="org-name" type="text" formControlName="name" [attr.maxlength]="limits.name" />
        @if (form.controls.name.touched && form.controls.name.hasError('required')) {
          <span class="le-error">Name is required.</span>
        }
        @if (form.controls.name.touched && form.controls.name.hasError('maxlength')) {
          <span class="le-error">Keep the name to {{ limits.name }} characters or fewer.</span>
        }
      </div>

      <div class="le-field">
        <label for="org-description">Description</label>
        <textarea
          id="org-description"
          formControlName="description"
          [attr.maxlength]="limits.description"
        ></textarea>
        <span class="le-hint">Optional.</span>
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
            <span>{{ initial() === null ? 'Create organization' : 'Save changes' }}</span>
          }
        </button>
      </div>
    </form>
  `,
  styles: `
    .actions {
      display: flex;
      gap: 8px;
      justify-content: flex-end;
    }
  `,
})
export class OrganizationFormComponent implements OnInit {
  readonly saving = input(false);
  /** Pre-fills the form for editing; null for a new organization. */
  readonly initial = input<OrganizationDto | null>(null);
  readonly save = output<CreateOrganizationCommand>();
  readonly cancel = output<void>();

  protected readonly limits = ORGANIZATION_LIMITS;

  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly form = this.fb.group({
    name: this.fb.control('', [
      Validators.required,
      Validators.maxLength(ORGANIZATION_LIMITS.name),
    ]),
    description: this.fb.control('', [Validators.maxLength(ORGANIZATION_LIMITS.description)]),
  });

  ngOnInit(): void {
    const initial = this.initial();
    if (initial !== null) {
      this.form.reset({ name: initial.name, description: initial.description });
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
    this.save.emit(this.form.getRawValue());
  }
}
