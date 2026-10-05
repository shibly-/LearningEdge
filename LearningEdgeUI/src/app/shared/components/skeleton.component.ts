import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Loading placeholder. The spec requires a skeleton, not a bare spinner. */
@Component({
  selector: 'app-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="wrap" role="status" aria-busy="true" [attr.aria-label]="label()">
      @for (row of rows(); track $index) {
        <span class="bar" [style.width.%]="row"></span>
      }
    </div>
  `,
  styles: `
    .wrap {
      display: flex;
      flex-direction: column;
      gap: 10px;
      padding: 4px 0;
    }

    .bar {
      height: 14px;
      border-radius: 6px;
      background: linear-gradient(
        90deg,
        var(--le-surface-sunken) 25%,
        var(--le-surface-muted) 37%,
        var(--le-surface-sunken) 63%
      );
      background-size: 400% 100%;
      animation: shimmer 1.3s ease-in-out infinite;
    }

    @keyframes shimmer {
      0% {
        background-position: 100% 0;
      }
      100% {
        background-position: 0 0;
      }
    }
  `,
})
export class SkeletonComponent {
  readonly count = input(4);
  readonly label = input('Loading content');

  /** Varied widths read as content rather than a progress bar. */
  rows(): readonly number[] {
    const widths = [92, 74, 86, 60, 80, 68];
    return Array.from({ length: this.count() }, (_, i) => widths[i % widths.length]);
  }
}
