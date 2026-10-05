import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** Error state: the message plus a retry (spec 7). */
@Component({
  selector: 'app-error-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="error" role="alert">
      <h3>{{ title() }}</h3>
      <p>{{ message() }}</p>
      <button type="button" class="retry" (click)="retry.emit()">Try again</button>
    </div>
  `,
  styles: `
    .error {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 8px;
      padding: 20px;
      background: var(--le-danger-bg);
      border: 1px solid var(--le-danger);
      border-radius: var(--le-radius);
    }

    h3 {
      margin: 0;
      font-size: 1rem;
      color: var(--le-danger);
    }

    p {
      margin: 0;
      color: var(--le-text);
    }

    .retry {
      padding: 7px 14px;
      border: 1px solid var(--le-danger);
      border-radius: var(--le-radius);
      background: transparent;
      color: var(--le-danger);
      font: inherit;
      cursor: pointer;
    }

    .retry:hover {
      background: rgb(179 38 30 / 8%);
    }
  `,
})
export class ErrorStateComponent {
  readonly title = input('Could not load this content');
  readonly message = input('Something went wrong. Please try again.');
  readonly retry = output<void>();
}
