import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';

/**
 * Errors go in an assertive alert region so they interrupt; everything else is
 * announced politely. Neither steals focus.
 */
@Component({
  selector: 'app-toast-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="stack">
      <div class="region" role="alert" aria-live="assertive" aria-atomic="false">
        @for (toast of errors(); track toast.id) {
          <ng-container *ngTemplateOutlet="item; context: { $implicit: toast }" />
        }
      </div>
      <div class="region" role="status" aria-live="polite" aria-atomic="false">
        @for (toast of others(); track toast.id) {
          <ng-container *ngTemplateOutlet="item; context: { $implicit: toast }" />
        }
      </div>
    </div>

    <ng-template #item let-toast>
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
    </ng-template>
  `,
  imports: [NgTemplateOutlet],
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

    .region {
      display: contents;
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
  protected readonly errors = computed(() =>
    this.service.toasts().filter((toast) => toast.kind === 'error'),
  );
  protected readonly others = computed(() =>
    this.service.toasts().filter((toast) => toast.kind !== 'error'),
  );
}
