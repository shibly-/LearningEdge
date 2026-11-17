import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { TECHNICAL_STACK } from './marketing-content';

@Component({
  selector: 'app-marketing-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './marketing-layout.component.html',
  styleUrl: './marketing-layout.component.scss',
})
export class MarketingLayoutComponent {
  protected readonly auth = inject(AuthService);
  protected readonly stack = TECHNICAL_STACK;
  protected readonly year = new Date().getFullYear();

  private readonly menu = signal(false);
  protected readonly menuOpen = this.menu.asReadonly();

  protected toggleMenu(): void {
    this.menu.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menu.set(false);
  }
}
