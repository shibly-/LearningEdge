import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TRAINING_LIMITS, type Training } from '../../core/models/training';
import { OrganizationContextService } from '../../core/services/organization-context.service';
import { CategoryStore } from '../../core/stores/category.store';
import { TrainingStore } from '../../core/stores/training.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';
import { TrainingFilesDialogComponent } from './training-files-dialog.component';

type ActiveFilter = 'all' | 'active' | 'inactive';

/** null: closed · 'new': creating · Training: editing that training. */
type Editor = null | 'new' | Training;

@Component({
  selector: 'app-training-list',
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
    TrainingFilesDialogComponent,
  ],
  templateUrl: './training-list.component.html',
  styleUrl: './training-list.component.scss',
})
export class TrainingListComponent {
  protected readonly store = inject(TrainingStore);
  protected readonly categories = inject(CategoryStore);
  protected readonly limits = TRAINING_LIMITS;

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly context = inject(OrganizationContextService);
  private readonly filter = signal<ActiveFilter>('all');

  protected readonly editor = signal<Editor>(null);
  protected readonly isEditing = computed(() => {
    const editor = this.editor();
    return editor !== null && editor !== 'new';
  });
  protected readonly filesFor = signal<Training | null>(null);
  protected readonly activeFilter = this.filter.asReadonly();

  protected readonly filters: readonly { readonly id: ActiveFilter; readonly label: string }[] = [
    { id: 'all', label: 'All' },
    { id: 'active', label: 'Active' },
    { id: 'inactive', label: 'Inactive' },
  ];

  protected readonly visible = computed(() => {
    const filter = this.filter();
    const all = this.store.items();
    const filtered =
      filter === 'all' ? all : all.filter((t) => t.isActive === (filter === 'active'));
    return [...filtered].sort((a, b) => a.name.localeCompare(b.name));
  });

  protected readonly noMatches = computed(
    () => this.store.status() === 'success' && !this.store.isEmpty() && this.visible().length === 0,
  );

  protected readonly form = this.fb.group({
    name: this.fb.control('', [Validators.required, Validators.maxLength(TRAINING_LIMITS.name)]),
    categoryId: this.fb.control('', [Validators.required]),
    description: this.fb.control('', [Validators.maxLength(TRAINING_LIMITS.description)]),
    isActive: this.fb.control(true),
  });

  constructor() {
    this.store.loadForActiveOrganization();
    if (this.categories.status() === 'idle') {
      this.categories.loadForActiveOrganization();
    }
  }

  protected categoryName(categoryId: string): string {
    return this.categories.findById(categoryId)?.name ?? 'Uncategorized';
  }

  protected setFilter(filter: ActiveFilter): void {
    this.filter.set(filter);
  }

  protected openCreate(): void {
    this.form.controls.categoryId.enable();
    this.form.reset({
      name: '',
      categoryId: this.categories.active()[0]?.id ?? '',
      description: '',
      isActive: true,
    });
    this.editor.set('new');
  }

  /** The API scopes a training to its category, so editing cannot move it. */
  protected openEdit(training: Training): void {
    this.form.reset({
      name: training.name,
      categoryId: training.categoryId,
      description: training.description,
      isActive: training.isActive,
    });
    this.form.controls.categoryId.disable();
    this.editor.set(training);
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
      this.store.update(
        {
          id: editor.id,
          organizationId: editor.organizationId,
          categoryId: editor.categoryId,
          name: value.name,
          description: value.description,
          isActive: value.isActive,
        },
        () => this.close(),
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
