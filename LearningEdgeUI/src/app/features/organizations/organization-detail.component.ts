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
import { toSignal } from '@angular/core/rxjs-interop';
import { map, type Subscription } from 'rxjs';
import type { CreateOrganizationCommand, OrganizationDto } from '../../core/models/organization';
import { OrganizationRepository } from '../../core/services/organization.repository';
import { describeError } from '../../core/stores/async-collection.store';
import { OrganizationStore } from '../../core/stores/organization.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state.component';
import { ModalShellComponent } from '../../shared/components/modal-shell.component';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SkeletonComponent } from '../../shared/components/skeleton.component';
import { OrganizationFormComponent } from './organization-form.component';

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
    ModalShellComponent,
    OrganizationFormComponent,
  ],
  template: `
    <app-page-header [title]="organization()?.name ?? 'Organization'" subtitle="Tenant detail">
      <a class="le-btn le-btn-secondary" routerLink="/platform/organizations">Back to list</a>
      @if (state() === 'loaded') {
        <button type="button" class="le-btn" (click)="editing.set(true)">Edit</button>
      }
    </app-page-header>

    @if (editing() && organization(); as current) {
      <app-modal-shell title="Edit organization" (close)="editing.set(false)">
        <app-organization-form
          [initial]="current"
          [saving]="store.isSaving()"
          (save)="saveEdit(current.id, $event)"
          (cancel)="editing.set(false)"
        />
      </app-modal-shell>
    }

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
  protected readonly store = inject(OrganizationStore);

  protected readonly editing = signal(false);
  private request: Subscription | null = null;

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
    // The router reuses this component across ids, so reload on every change.
    effect(() => {
      this.organizationId();
      untracked(() => this.load());
    });
    this.destroyRef.onDestroy(() => this.request?.unsubscribe());
  }

  protected saveEdit(id: string, command: CreateOrganizationCommand): void {
    this.store.update(id, command, (organization) => {
      this.organizationState.set(organization);
      this.editing.set(false);
    });
  }

  protected load(): void {
    this.request?.unsubscribe();
    this.editing.set(false);

    const id = this.organizationId();
    if (id === null) {
      this.viewState.set('missing');
      return;
    }

    this.viewState.set('loading');
    this.failure.set(null);

    this.request = this.repository.getById(id).subscribe({
      next: (organization) => {
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
