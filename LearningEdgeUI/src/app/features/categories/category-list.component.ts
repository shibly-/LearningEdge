import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { OrganizationContextService } from '../../core/services/organization-context.service';
import { CategoryStore } from '../../core/stores/category.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';

@Component({
  selector: 'app-category-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
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
            <h2>{{ category.name }}</h2>
            <p class="desc">{{ category.description || 'No description.' }}</p>
            <p class="meta">{{ category.trainingCount }} training(s)</p>
            <a [routerLink]="['/trainings/category', category.id]">View trainings</a>
          </article>
        }
      </div>
    }

    @if (isCreating()) {
      <app-modal-shell title="New category" (close)="closeCreate()">
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div
            class="le-field"
            [class.is-invalid]="form.controls.name.touched && form.controls.name.invalid"
          >
            <label for="category-name">Name</label>
            <input id="category-name" type="text" formControlName="name" />
            @if (form.controls.name.touched && form.controls.name.hasError('required')) {
              <span class="le-error">Name is required.</span>
            }
          </div>

          <div class="le-field">
            <label for="category-description">Description</label>
            <textarea id="category-description" formControlName="description"></textarea>
          </div>

          <div class="actions">
            <button type="button" class="le-btn le-btn-secondary" (click)="closeCreate()">
              Cancel
            </button>
            <button type="submit" class="le-btn" [disabled]="store.isSaving()">
              @if (store.isSaving()) {
                <app-spinner [size]="14" label="Saving" />
                <span>Saving…</span>
              } @else {
                <span>Create category</span>
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

    h2 {
      margin: 0;
      font-size: 1.05rem;
    }

    .desc {
      margin: 0;
      color: var(--le-text-muted);
    }

    .meta {
      margin: 0;
      font-size: 0.85rem;
      color: var(--le-text-muted);
    }

    a {
      margin-top: 6px;
      color: var(--le-brand-600);
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

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly context = inject(OrganizationContextService);
  private readonly creating = signal(false);

  protected readonly isCreating = this.creating.asReadonly();
  protected readonly canCreate = computed(() => this.context.activeOrganizationId() !== null);

  protected readonly form = this.fb.group({
    name: this.fb.control('', [Validators.required]),
    description: this.fb.control(''),
  });

  constructor() {
    this.store.loadForActiveOrganization();
  }

  protected openCreate(): void {
    this.form.reset({ name: '', description: '' });
    this.creating.set(true);
  }

  protected closeCreate(): void {
    this.creating.set(false);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      return;
    }

    this.store.create({ ...this.form.getRawValue(), organizationId }, () =>
      this.creating.set(false),
    );
  }

  protected reload(): void {
    this.store.loadForActiveOrganization();
  }
}
