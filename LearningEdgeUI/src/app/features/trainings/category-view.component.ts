import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { CategoryStore } from '../../core/stores/category.store';
import { TrainingStore } from '../../core/stores/training.store';
import { BadgeComponent } from '../../shared/components/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
@Component({
  selector: 'app-category-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    BadgeComponent,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  template: `
    <app-page-header [title]="categoryName()" subtitle="Trainings in this category">
      <a class="le-btn le-btn-secondary" routerLink="/categories">All categories</a>
    </app-page-header>

    @if (store.isLoading()) {
      <div class="le-card" style="padding: 18px">
        <app-skeleton [count]="4" label="Loading trainings" />
      </div>
    } @else if (store.hasError()) {
      <app-error-state [message]="store.error() ?? ''" (retry)="reload()" />
    } @else if (store.isEmpty()) {
      <app-empty-state
        title="No trainings in this category"
        message="Create a training and assign it to this category."
      />
    } @else {
      <div class="le-card le-table-wrap">
        <table class="le-table">
          <caption>
            {{
              store.count()
            }}
            training(s)
          </caption>
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Description</th>
              <th scope="col">Files</th>
              <th scope="col">Status</th>
            </tr>
          </thead>
          <tbody>
            @for (training of store.items(); track training.id) {
              <tr>
                <th scope="row">{{ training.name }}</th>
                <td>{{ training.description || '—' }}</td>
                <td>{{ training.files.length }}</td>
                <td>
                  <app-badge
                    [status]="training.isActive ? 'on' : 'off'"
                    [text]="training.isActive ? 'Active' : 'Inactive'"
                  />
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class CategoryViewComponent {
  protected readonly store = inject(TrainingStore);

  private readonly route = inject(ActivatedRoute);
  private readonly categories = inject(CategoryStore);

  private readonly categoryId = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('categoryId'))),
    { initialValue: null },
  );

  protected readonly categoryName = computed(() => {
    const id = this.categoryId();
    if (id === null) {
      return 'Category';
    }
    return this.categories.findById(id)?.name ?? 'Category';
  });

  constructor() {
    if (this.categories.status() === 'idle') {
      this.categories.loadForActiveOrganization();
    }
    // Reload whenever the route parameter changes.
    effect(() => {
      const id = this.categoryId();
      if (id !== null) {
        this.store.loadForCategory(id);
      }
    });
  }

  protected reload(): void {
    const id = this.categoryId();
    if (id !== null) {
      this.store.loadForCategory(id);
    }
  }
}
