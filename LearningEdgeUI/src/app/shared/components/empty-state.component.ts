import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** Empty state: explanatory copy plus an optional primary action (spec 7). */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty">
      <h3>{{ title() }}</h3>
      <p>{{ message() }}</p>
      @if (actionLabel() !== null) {
        <button type="button" class="action" (click)="action.emit()">{{ actionLabel() }}</button>
      }
    </div>
  `,
  styles: `
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
      padding: 48px 24px;
      text-align: center;
      background: var(--le-surface);
      border: 1px dashed var(--le-border);
      border-radius: var(--le-radius);
    }

    h3 {
      margin: 0;
      font-size: 1rem;
    }

    p {
      margin: 0;
      max-width: 44ch;
      color: var(--le-text-muted);
    }

    .action {
      margin-top: 8px;
      padding: 8px 16px;
      border: 0;
      border-radius: var(--le-radius);
      background: var(--le-brand-600);
      color: var(--le-text-inverse);
      font: inherit;
      cursor: pointer;
    }

    .action:hover {
      background: var(--le-brand-700);
    }
  `,
})
export class EmptyStateComponent {
  readonly title = input('Nothing here yet');
  readonly message = input('There is no data to show.');
  readonly actionLabel = input<string | null>(null);
  readonly action = output<void>();
}
