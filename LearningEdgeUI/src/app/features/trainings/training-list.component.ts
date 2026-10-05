import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
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
import { DurationPipe } from '../../shared/pipes/duration.pipe';

type StatusFilter = 'all' | 'published' | 'draft' | 'archived';

@Component({
  selector: 'app-training-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DurationPipe,
    BadgeComponent,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    ModalShellComponent,
    SpinnerComponent,
  ],
  templateUrl: './training-list.component.html',
  styleUrl: './training-list.component.scss',
})
export class TrainingListComponent {
  protected readonly store = inject(TrainingStore);
  protected readonly categories = inject(CategoryStore);

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly context = inject(OrganizationContextService);
  private readonly creating = signal(false);
  private readonly statusFilter = signal<StatusFilter>('all');

  protected readonly isCreating = this.creating.asReadonly();
  protected readonly activeStatus = this.statusFilter.asReadonly();

  protected readonly statuses: readonly { readonly id: StatusFilter; readonly label: string }[] = [
    { id: 'all', label: 'All' },
    { id: 'published', label: 'Published' },
    { id: 'draft', label: 'Drafts' },
    { id: 'archived', label: 'Archived' },
  ];

  protected readonly visible = computed(() => {
    const status = this.statusFilter();
    const all = this.store.items();
    const filtered = status === 'all' ? all : all.filter((t) => t.status === status);
    return [...filtered].sort((a, b) => a.title.localeCompare(b.title));
  });

  protected readonly noMatches = computed(
    () => this.store.status() === 'success' && !this.store.isEmpty() && this.visible().length === 0,
  );

  protected readonly form = this.fb.group({
    title: this.fb.control('', [Validators.required]),
    categoryId: this.fb.control('', [Validators.required]),
    description: this.fb.control(''),
    durationMinutes: this.fb.control(30, [Validators.required, Validators.min(1)]),
    passMark: this.fb.control(70, [Validators.required, Validators.min(0), Validators.max(100)]),
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

  protected setStatus(status: StatusFilter): void {
    this.statusFilter.set(status);
  }

  protected openCreate(): void {
    this.form.reset({
      title: '',
      categoryId: this.categories.items()[0]?.id ?? '',
      description: '',
      durationMinutes: 30,
      passMark: 70,
    });
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

    const value = this.form.getRawValue();
    this.store.create(
      {
        organizationId,
        categoryId: value.categoryId,
        title: value.title,
        description: value.description,
        durationMinutes: Number(value.durationMinutes),
        passMark: Number(value.passMark),
      },
      () => this.creating.set(false),
    );
  }

  protected reload(): void {
    this.store.loadForActiveOrganization();
  }
}
