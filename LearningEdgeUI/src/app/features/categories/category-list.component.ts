import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CATEGORY_LIMITS, type Category } from '../../core/models/category';
import { OrganizationContextService } from '../../core/services/organization-context.service';
import { CategoryStore } from '../../core/stores/category.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';

/** null: closed · 'new': creating · Category: editing that category. */
type Editor = null | 'new' | Category;

@Component({
  selector: 'app-category-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    BadgeComponent,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    ModalShellComponent,
    SpinnerComponent,
  ],
  template: `
    <app-page-header title="Training categories" subtitle="Group related trainings together.">
      <button type="button" class="le-btn" (click)="openCreate()">New category</button>
    </app-page-header>

    @if (store.isLoading()) {
      <div class="le-card" style="padding: 18px">
        <app-skeleton [count]="4" label="Loading categories" />
      </div>
    } @else if (store.hasError()) {
      <app-error-state [message]="store.error() ?? ''" (retry)="reload()" />
    } @else if (store.isEmpty()) {
      <app-empty-state
        title="No categories yet"
        message="Categories organize your training catalogue. Create the first one to begin."
        actionLabel="New category"
        (action)="openCreate()"
      />
    } @else {
      <div class="grid">
        @for (category of store.sortedByName(); track category.id) {
          <article class="le-card card">
            <div class="title-row">
              <h2>{{ category.name }}</h2>
              <app-badge
                [status]="category.isActive ? 'on' : 'off'"
                [text]="category.isActive ? 'Active' : 'Inactive'"
              />
            </div>
            <p class="desc">{{ category.description || 'No description.' }}</p>
            <div class="links">
              <a [routerLink]="['/trainings/category', category.id]">View trainings</a>
              <button
                type="button"
                class="le-btn le-btn-secondary le-btn-sm"
                [attr.aria-label]="'Edit ' + category.name"
                (click)="openEdit(category)"
              >
                Edit
              </button>
            </div>
          </article>
        }
      </div>
    }

    @if (editor() !== null) {
      <app-modal-shell [title]="isEditing() ? 'Edit category' : 'New category'" (close)="close()">
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div
            class="le-field"
            [class.is-invalid]="form.controls.name.touched && form.controls.name.invalid"
          >
            <label for="category-name">Name</label>
            <input
              id="category-name"
              type="text"
              formControlName="name"
              [attr.maxlength]="limits.name"
            />
            @if (form.controls.name.touched && form.controls.name.hasError('required')) {
              <span class="le-error">Name is required.</span>
            }
          </div>

          <div class="le-field">
            <label for="category-description">Description</label>
            <textarea
              id="category-description"
              formControlName="description"
              [attr.maxlength]="limits.description"
            ></textarea>
          </div>

          <label class="check">
            <input type="checkbox" formControlName="isActive" />
            <span>Active</span>
          </label>

          <div class="actions">
            <button type="button" class="le-btn le-btn-secondary" (click)="close()">Cancel</button>
            <button type="submit" class="le-btn" [disabled]="store.isSaving()">
              @if (store.isSaving()) {
                <app-spinner [size]="14" label="Saving" />
                <span>Saving…</span>
              } @else {
                <span>{{ isEditing() ? 'Save changes' : 'Create category' }}</span>
              }
            </button>
          </div>
        </form>
      </app-modal-shell>
    }
  `,
  styles: `
    .grid {
      display: grid;
      gap: 14px;
      grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
    }

    .card {
      display: flex;
      flex-direction: column;
      gap: 6px;
      padding: 18px;
    }

    .title-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
    }

    h2 {
      margin: 0;
      font-size: 1.05rem;
    }

    .desc {
      margin: 0;
      color: var(--le-text-muted);
    }

    .links {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-top: 6px;
    }

    a {
      color: var(--le-brand-600);
    }

    .check {
      display: flex;
      gap: 8px;
      align-items: center;
      margin-bottom: 16px;
      font-weight: 400;
    }

    .actions {
      display: flex;
      gap: 8px;
      justify-content: flex-end;
    }
  `,
})
export class CategoryListComponent {
  protected readonly store = inject(CategoryStore);
  protected readonly limits = CATEGORY_LIMITS;

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly context = inject(OrganizationContextService);

  protected readonly editor = signal<Editor>(null);
  protected readonly isEditing = computed(() => {
    const editor = this.editor();
    return editor !== null && editor !== 'new';
  });

  protected readonly form = this.fb.group({
    name: this.fb.control('', [Validators.required, Validators.maxLength(CATEGORY_LIMITS.name)]),
    description: this.fb.control('', [Validators.maxLength(CATEGORY_LIMITS.description)]),
    isActive: this.fb.control(true),
  });

  constructor() {
    this.store.loadForActiveOrganization();
  }

  protected openCreate(): void {
    this.form.reset({ name: '', description: '', isActive: true });
    this.editor.set('new');
  }

  protected openEdit(category: Category): void {
    this.form.reset({
      name: category.name,
      description: category.description,
      isActive: category.isActive,
    });
    this.editor.set(category);
  }

  protected close(): void {
    this.editor.set(null);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const editor = this.editor();
    const value = this.form.getRawValue();
    if (editor !== null && editor !== 'new') {
      this.store.update({ ...value, id: editor.id, organizationId: editor.organizationId }, () =>
        this.close(),
      );
      return;
    }

    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      return;
    }
    this.store.create({ ...value, organizationId }, () => this.close());
  }

  protected reload(): void {
    this.store.loadForActiveOrganization();
  }
}
