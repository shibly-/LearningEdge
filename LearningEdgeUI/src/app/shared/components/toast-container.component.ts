import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';

/** aria-live="polite" so screen readers announce toasts without stealing focus. */
@Component({
  selector: 'app-toast-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="stack" role="status" aria-live="polite" aria-atomic="false">
      @for (toast of toasts(); track toast.id) {
        <div class="toast" [class]="'kind-' + toast.kind">
          <span class="message">{{ toast.message }}</span>
          <button
            type="button"
            class="dismiss"
            [attr.aria-label]="'Dismiss: ' + toast.message"
            (click)="service.dismiss(toast.id)"
          >
            &times;
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .stack {
      position: fixed;
      right: 16px;
      bottom: 16px;
      z-index: 60;
      display: flex;
      flex-direction: column;
      gap: 10px;
      max-width: min(420px, calc(100vw - 32px));
    }

    .toast {
      display: flex;
      gap: 12px;
      align-items: flex-start;
      padding: 12px 14px;
      border: 1px solid;
      border-radius: var(--le-radius);
      background: var(--le-surface);
      box-shadow: var(--le-shadow);
    }

    .message {
      flex: 1;
    }

    .kind-success {
      border-color: var(--le-success);
      background: var(--le-success-bg);
      color: var(--le-success);
    }

    .kind-error {
      border-color: var(--le-danger);
      background: var(--le-danger-bg);
      color: var(--le-danger);
    }

    .kind-warning {
      border-color: var(--le-warning);
      background: var(--le-warning-bg);
      color: var(--le-warning);
    }

    .kind-info {
      border-color: var(--le-info);
      background: var(--le-info-bg);
      color: var(--le-info);
    }

    .dismiss {
      border: 0;
      background: transparent;
      color: inherit;
      font-size: 1.1rem;
      line-height: 1;
      cursor: pointer;
    }
  `,
})
export class ToastContainerComponent {
  protected readonly service = inject(ToastService);
  protected readonly toasts = this.service.toasts;
}
