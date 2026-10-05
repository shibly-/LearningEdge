import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { OrganizationContextService } from '../core/services/organization-context.service';
import { OrganizationStore } from '../core/stores/organization.store';

/**
 * SysAdmin-only tenant switcher. Switching clears every tenant-scoped store via
 * OrganizationContextService (spec 4.3).
 */
@Component({
  selector: 'app-org-switcher',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="switcher">
      <label for="org-switcher">Organization</label>
      <select id="org-switcher" [value]="activeId() ?? ''" (change)="onChange($event)">
        <option value="">All organizations</option>
        @for (org of organizations(); track org.id) {
          <option [value]="org.id">{{ org.name }}</option>
        }
      </select>
    </div>
  `,
  styles: `
    .switcher {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    label {
      font-size: 0.82rem;
      font-weight: 600;
      color: var(--le-text-muted);
    }

    select {
      padding: 6px 10px;
      border: 1px solid var(--le-border);
      border-radius: var(--le-radius);
      background: var(--le-surface);
      color: var(--le-text);
      font: inherit;
      max-width: 220px;
    }
  `,
})
export class OrgSwitcherComponent {
  private readonly context = inject(OrganizationContextService);
  private readonly store = inject(OrganizationStore);

  protected readonly activeId = this.context.activeOrganizationId;
  protected readonly organizations = computed(() => this.store.sortedByName());

  constructor() {
    if (this.store.status() === 'idle') {
      this.store.loadAll();
    }
  }

  protected onChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.context.switchOrganization(value === '' ? null : value);
  }
}
