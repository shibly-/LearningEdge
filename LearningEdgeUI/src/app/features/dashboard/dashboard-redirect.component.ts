import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { homeRouteFor } from '../../core/models/user-role';
import { SpinnerComponent } from '../../shared/components/spinner.component';

/** /dashboard resolves to the right landing route per role (spec 6). */
@Component({
  selector: 'app-dashboard-redirect',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SpinnerComponent],
  template: `
    <div class="pending">
      <app-spinner [size]="22" label="Opening your dashboard" />
      <p>Opening your dashboard…</p>
    </div>
  `,
  styles: `
    .pending {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 10px;
      padding: 64px 0;
      color: var(--le-text-muted);
    }
  `,
})
export class DashboardRedirectComponent {
  constructor() {
    const auth = inject(AuthService);
    const router = inject(Router);
    const role = auth.role();
    void router.navigateByUrl(role === null ? '/login' : homeRouteFor(role), {
      replaceUrl: true,
    });
  }
}
