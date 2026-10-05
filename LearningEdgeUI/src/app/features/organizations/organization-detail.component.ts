import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import type { OrganizationDto } from '../../core/models/organization';
import { OrganizationRepository } from '../../core/services/organization.repository';
import { describeError } from '../../core/stores/async-collection.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

type ViewState = 'loading' | 'loaded' | 'missing' | 'error';

@Component({
  selector: 'app-organization-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    PageHeaderComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  template: `
    <app-page-header [title]="organization()?.name ?? 'Organization'" subtitle="Tenant detail">
      <a class="le-btn le-btn-secondary" routerLink="/platform/organizations">Back to list</a>
    </app-page-header>

    @switch (state()) {
      @case ('loading') {
        <div class="le-card" style="padding: 18px">
          <app-skeleton [count]="3" label="Loading organization" />
        </div>
      }
      @case ('error') {
        <app-error-state [message]="errorMessage() ?? ''" (retry)="load()" />
      }
      @case ('missing') {
        <app-empty-state
          title="Organization not found"
          message="It may have been removed, or the identifier in the URL is incorrect."
        />
      }
      @case ('loaded') {
        <dl class="le-card detail">
          <dt>Name</dt>
          <dd>{{ organization()?.name }}</dd>
          <dt>Description</dt>
          <dd>{{ organization()?.description || '—' }}</dd>
          <dt>Identifier</dt>
          <dd>
            <code>{{ organization()?.id }}</code>
          </dd>
        </dl>
      }
    }
  `,
  styles: `
    .detail {
      display: grid;
      grid-template-columns: 180px 1fr;
      gap: 0;
      margin: 0;
      padding: 4px 0;
    }

    dt,
    dd {
      margin: 0;
      padding: 14px 18px;
      border-bottom: 1px solid var(--le-border);
    }

    dt {
      font-weight: 600;
      color: var(--le-text-muted);
    }

    .detail > :nth-last-child(1),
    .detail > :nth-last-child(2) {
      border-bottom: 0;
    }

    @media (max-width: 640px) {
      .detail {
        grid-template-columns: 1fr;
      }

      dt {
        padding-bottom: 0;
        border-bottom: 0;
      }
    }
  `,
})
export class OrganizationDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly repository = inject(OrganizationRepository);
  private readonly destroyRef = inject(DestroyRef);

  private readonly organizationState = signal<OrganizationDto | null>(null);
  private readonly viewState = signal<ViewState>('loading');
  private readonly failure = signal<string | null>(null);

  protected readonly organization = this.organizationState.asReadonly();
  protected readonly state = this.viewState.asReadonly();
  protected readonly errorMessage = this.failure.asReadonly();

  private readonly organizationId = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('organizationId'))),
    { initialValue: null },
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    const id = this.organizationId();
    if (id === null) {
      this.viewState.set('missing');
      return;
    }

    this.viewState.set('loading');
    this.failure.set(null);

    this.repository
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (organization) => {
          // success:false on HTTP 200 means absent, not an error (spec 3.3).
          this.organizationState.set(organization);
          this.viewState.set(organization === null ? 'missing' : 'loaded');
        },
        error: (error: unknown) => {
          this.failure.set(describeError(error));
          this.viewState.set('error');
        },
      });
  }
}
