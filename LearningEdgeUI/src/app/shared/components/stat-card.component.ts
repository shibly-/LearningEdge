import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-stat-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="card">
      <p class="label">{{ label() }}</p>
      <p class="value">{{ value() }}</p>
      @if (hint() !== null) {
        <p class="hint">{{ hint() }}</p>
      }
    </div>
  `,
  styles: `
    .card {
      padding: 16px 18px;
      background: var(--le-surface);
      border: 1px solid var(--le-border);
      border-radius: var(--le-radius);
      box-shadow: var(--le-shadow);
    }

    .label {
      margin: 0 0 6px;
      font-size: 0.8rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--le-text-muted);
    }

    .value {
      margin: 0;
      font-size: 1.75rem;
      font-weight: 700;
      line-height: 1.1;
    }

    .hint {
      margin: 6px 0 0;
      font-size: 0.85rem;
      color: var(--le-text-muted);
    }
  `,
})
export class StatCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly hint = input<string | null>(null);
}
