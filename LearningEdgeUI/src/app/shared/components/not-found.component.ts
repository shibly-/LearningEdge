import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { homeRouteFor } from '../../core/models/user-role';

@Component({
  selector: 'app-not-found',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="wrap">
      <p class="code">404</p>
      <h1>Page not found</h1>
      <p class="message">
        The page you asked for does not exist, or you followed a link that is out of date.
      </p>
      <a class="le-btn" [routerLink]="home">Go to my dashboard</a>
    </div>
  `,
  styles: `
    .wrap {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
      padding: 72px 24px;
      text-align: center;
    }

    .code {
      margin: 0;
      font-size: 3rem;
      font-weight: 800;
      color: var(--le-brand-600);
      line-height: 1;
    }

    h1 {
      margin: 0;
      font-size: 1.3rem;
    }

    .message {
      margin: 0 0 8px;
      max-width: 46ch;
      color: var(--le-text-muted);
    }
  `,
})
export class NotFoundComponent {
  private readonly auth = inject(AuthService);

  protected readonly home = (() => {
    const role = this.auth.role();
    return role === null ? '/login' : homeRouteFor(role);
  })();
}
