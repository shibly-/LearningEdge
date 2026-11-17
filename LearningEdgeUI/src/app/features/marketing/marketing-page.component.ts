import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MARKETING_PAGES } from './marketing-content';
import type { MarketingPageId } from './marketing-match';

@Component({
  selector: 'app-marketing-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './marketing-page.component.html',
  styleUrl: './marketing-page.component.scss',
})
export class MarketingPageComponent {
  /** Bound from the route `data.page` value. */
  readonly page = input.required<MarketingPageId>();

  protected readonly content = computed(() => MARKETING_PAGES[this.page()]);
  protected readonly year = new Date().getFullYear();
}
