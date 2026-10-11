import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map, type Subscription } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth/auth.service';
import type { Training, TrainingFile } from '../../core/models/training';
import { OrganizationContextService } from '../../core/services/organization-context.service';
import { TrainingRepository } from '../../core/services/training.repository';
import { describeError } from '../../core/stores/async-collection.store';
import { CategoryStore } from '../../core/stores/category.store';
import { TrainingStore } from '../../core/stores/training.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { SpinnerComponent } from '../../shared/components/spinner.component';

type ViewState = 'loading' | 'loaded' | 'missing' | 'error';

@Component({
  selector: 'app-training-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    BadgeComponent,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SpinnerComponent,
  ],
  templateUrl: './training-view.component.html',
  styleUrl: './training-view.component.scss',
})
export class TrainingViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly repository = inject(TrainingRepository);
  private readonly context = inject(OrganizationContextService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly categories = inject(CategoryStore);
  protected readonly store = inject(TrainingStore);

  private request: Subscription | null = null;

  private readonly trainingState = signal<Training | null>(null);
  private readonly viewState = signal<ViewState>('loading');
  private readonly failure = signal<string | null>(null);

  protected readonly training = this.trainingState.asReadonly();
  protected readonly state = this.viewState.asReadonly();
  protected readonly errorMessage = this.failure.asReadonly();

  private readonly categoryId = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('categoryId'))),
    { initialValue: null },
  );
  private readonly trainingId = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('trainingId'))),
    { initialValue: null },
  );

  constructor() {
    if (this.categories.status() === 'idle') {
      this.categories.loadForActiveOrganization();
    }
    effect(() => {
      this.categoryId();
      this.trainingId();
      untracked(() => this.load());
    });
    this.destroyRef.onDestroy(() => this.request?.unsubscribe());
  }

  protected headingText(): string {
    return this.trainingState()?.name ?? 'Training';
  }

  protected categoryName(): string {
    const categoryId = this.trainingState()?.categoryId;
    if (categoryId === undefined) {
      return 'Uncategorized';
    }
    return this.categories.findById(categoryId)?.name ?? 'Uncategorized';
  }

  protected extract(file: TrainingFile): void {
    const training = this.trainingState();
    const user = this.auth.currentUser();
    if (training === null || user === null) {
      return;
    }

    this.store.extractFile({
      categoryId: training.categoryId,
      trainingId: training.id,
      fileId: file.id,
      requestedByUserId: user.id,
      fileName: file.fileName,
    });
  }

  protected load(): void {
    this.request?.unsubscribe();

    const categoryId = this.categoryId();
    const trainingId = this.trainingId();
    const organizationId = this.context.activeOrganizationId();
    if (categoryId === null || trainingId === null || organizationId === null) {
      this.trainingState.set(null);
      this.viewState.set('missing');
      return;
    }

    this.viewState.set('loading');
    this.failure.set(null);

    this.request = this.repository.getById(organizationId, categoryId, trainingId).subscribe({
      next: (training) => {
        this.trainingState.set(training);
        this.viewState.set(training === null ? 'missing' : 'loaded');
      },
      error: (error: unknown) => {
        this.failure.set(describeError(error));
        this.viewState.set('error');
      },
    });
  }
}
