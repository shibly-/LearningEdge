import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { badgeToneFor } from '../pipes/status-badge.pipe';
import type { BadgeTone } from '../pipes/status-badge.pipe';

/**
 * Always renders the label text, so status is never conveyed by colour alone.
 */
@Component({
  selector: 'app-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [class]="'tone-' + tone()">{{ label() }}</span>`,
  styles: `
    .badge {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 999px;
      font-size: 0.78rem;
      font-weight: 600;
      white-space: nowrap;
      text-transform: capitalize;
    }

    .tone-success {
      background: var(--le-success-bg);
      color: var(--le-success);
    }

    .tone-warning {
      background: var(--le-warning-bg);
      color: var(--le-warning);
    }

    .tone-danger {
      background: var(--le-danger-bg);
      color: var(--le-danger);
    }

    .tone-info {
      background: var(--le-info-bg);
      color: var(--le-info);
    }

    .tone-neutral {
      background: var(--le-surface-sunken);
      color: var(--le-text-muted);
    }
  `,
})
export class BadgeComponent {
  readonly status = input.required<string>();
  readonly text = input<string | null>(null);

  readonly tone = computed<BadgeTone>(() => badgeToneFor(this.status()));
  readonly label = computed(() => this.text() ?? this.status().replace(/-/g, ' '));
}
