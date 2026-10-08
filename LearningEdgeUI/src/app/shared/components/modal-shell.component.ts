import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Traps focus, restores it on close, and closes on Escape (spec 7).
 */
@Component({
  selector: 'app-modal-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(keydown.escape)': 'close.emit()',
    '(keydown.tab)': 'onTab($event)',
  },
  template: `
    <div class="backdrop" (click)="close.emit()"></div>
    <div
      #panel
      class="panel"
      role="dialog"
      aria-modal="true"
      [attr.aria-labelledby]="headingId"
      tabindex="-1"
    >
      <header>
        <h2 [id]="headingId">{{ title() }}</h2>
        <button type="button" class="close" aria-label="Close dialog" (click)="close.emit()">
          &times;
        </button>
      </header>
      <div class="body">
        <ng-content />
      </div>
    </div>
  `,
  styles: `
    :host {
      position: fixed;
      inset: 0;
      z-index: 70;
      display: grid;
      place-items: center;
      padding: 20px;
    }

    .backdrop {
      position: absolute;
      inset: 0;
      background: rgb(17 22 31 / 45%);
    }

    .panel {
      position: relative;
      width: min(560px, 100%);
      max-height: calc(100vh - 80px);
      overflow: auto;
      background: var(--le-surface);
      border-radius: var(--le-radius);
      box-shadow: var(--le-shadow);
    }

    header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px 18px;
      border-bottom: 1px solid var(--le-border);
    }

    h2 {
      margin: 0;
      font-size: 1.1rem;
    }

    .close {
      border: 0;
      background: transparent;
      font-size: 1.3rem;
      line-height: 1;
      cursor: pointer;
    }

    .body {
      padding: 18px;
    }
  `,
})
export class ModalShellComponent implements AfterViewInit, OnDestroy {
  readonly title = input.required<string>();
  readonly close = output<void>();

  protected readonly headingId = `modal-${Math.random().toString(36).slice(2, 9)}`;

  private readonly panel = viewChild.required<ElementRef<HTMLElement>>('panel');
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly previouslyFocused = document.activeElement as HTMLElement | null;

  /** Elements this dialog made inert, so only those are restored on close. */
  private readonly inerted: HTMLElement[] = [];

  ngAfterViewInit(): void {
    this.inertBackground();
    const panel = this.panel().nativeElement;
    // Land on the first field rather than the header's close button.
    const body = panel.querySelector<HTMLElement>('.body');
    const first =
      this.focusable().find((element) => body?.contains(element)) ?? this.focusable()[0] ?? panel;
    first.focus();
  }

  ngOnDestroy(): void {
    for (const element of this.inerted) {
      element.inert = false;
    }
    this.previouslyFocused?.focus();
  }

  /**
   * Makes every sibling along the path to <body> inert. Toasts stay live so
   * save errors are still announced and can be dismissed.
   */
  private inertBackground(): void {
    let node: HTMLElement | null = this.host.nativeElement as HTMLElement;
    while (node !== null && node !== document.body) {
      const parent: HTMLElement | null = node.parentElement;
      for (const sibling of Array.from(parent?.children ?? [])) {
        if (
          sibling !== node &&
          sibling instanceof HTMLElement &&
          !sibling.inert &&
          sibling.tagName !== 'APP-TOAST-CONTAINER' &&
          sibling.tagName !== 'SCRIPT'
        ) {
          sibling.inert = true;
          this.inerted.push(sibling);
        }
      }
      node = parent;
    }
  }

  protected onTab(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    const items = this.focusable();
    if (items.length === 0) {
      return;
    }

    const first = items[0];
    const last = items[items.length - 1];
    const active = document.activeElement;

    if (keyboardEvent.shiftKey && active === first) {
      keyboardEvent.preventDefault();
      last.focus();
      return;
    }
    if (!keyboardEvent.shiftKey && active === last) {
      keyboardEvent.preventDefault();
      first.focus();
    }
  }

  private focusable(): readonly HTMLElement[] {
    const root = this.host.nativeElement as HTMLElement;
    return Array.from(root.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(
      (element) => element.offsetParent !== null,
    );
  }
}
