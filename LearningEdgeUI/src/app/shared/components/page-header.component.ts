import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="header">
      <div>
        <h1>{{ title() }}</h1>
        @if (subtitle() !== null) {
          <p>{{ subtitle() }}</p>
        }
      </div>
      <div class="actions">
        <ng-content />
      </div>
    </header>
  `,
  styles: `
    .header {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      align-items: flex-start;
      justify-content: space-between;
      margin-bottom: 20px;
    }

    h1 {
      margin: 0;
      font-size: 1.4rem;
    }

    p {
      margin: 4px 0 0;
      color: var(--le-text-muted);
    }

    .actions {
      display: flex;
      gap: 8px;
      align-items: center;
    }
  `,
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
}
