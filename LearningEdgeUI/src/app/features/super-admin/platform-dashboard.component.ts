import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { OrganizationStore } from '../../core/stores/organization.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { StatCardComponent } from '../../shared/components/stat-card.component';

@Component({
  selector: 'app-platform-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    PageHeaderComponent,
    StatCardComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  template: `
    <app-page-header title="Platform overview" subtitle="Super Admin scope — all organizations">
      <a class="le-btn" routerLink="/platform/organizations">Manage organizations</a>
    </app-page-header>

    @if (store.isLoading()) {
      <div class="le-card" style="padding: 18px">
        <app-skeleton [count]="4" label="Loading platform metrics" />
      </div>
    } @else if (store.hasError()) {
      <app-error-state [message]="store.error() ?? ''" (retry)="reload()" />
    } @else if (store.isEmpty()) {
      <app-empty-state
        title="No organizations yet"
        message="Create an organization, then provision its first administrator."
        actionLabel="Go to organizations"
        (action)="goToOrganizations()"
      />
    } @else {
      <div class="le-grid-stats">
        <app-stat-card label="Organizations" [value]="store.count()" />
        <app-stat-card
          label="Described"
          [value]="describedCount()"
          hint="Organizations with a description set"
        />
        <app-stat-card label="Newest" [value]="newestName()" />
      </div>

      <section class="le-card le-table-wrap">
        <table class="le-table">
          <caption>
            All organizations. Per-tenant metrics need list endpoints that the API does not expose
            yet.
          </caption>
          <thead>
            <tr>
              <th scope="col">Organization</th>
              <th scope="col">Description</th>
              <th scope="col"><span class="le-visually-hidden">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            @for (org of store.sortedByName(); track org.id) {
              <tr>
                <th scope="row">{{ org.name }}</th>
                <td>{{ org.description || '—' }}</td>
                <td><a [routerLink]="['/platform/organizations', org.id]">View</a></td>
              </tr>
            }
          </tbody>
        </table>
      </section>
    }
  `,
})
export class PlatformDashboardComponent {
  protected readonly store = inject(OrganizationStore);
  private readonly router = inject(Router);

  protected readonly describedCount = computed(
    () => this.store.items().filter((org) => org.description.trim().length > 0).length,
  );

  protected readonly newestName = computed(() => {
    const items = this.store.items();
    return items.length === 0 ? '—' : items[items.length - 1].name;
  });

  constructor() {
    if (this.store.status() === 'idle') {
      this.store.loadAll();
    }
  }

  protected reload(): void {
    this.store.loadAll();
  }

  protected goToOrganizations(): void {
    void this.router.navigate(['/platform/organizations']);
  }
}
