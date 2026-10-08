import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../core/auth/auth.service';
import { OrganizationContextService } from '../core/services/organization-context.service';
import { OrganizationStore } from '../core/stores/organization.store';
import { roleLabel } from '../core/models/user-role';
import { OrgSwitcherComponent } from './org-switcher.component';
import { navItemsFor } from './nav-items';

@Component({
  selector: 'app-main-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, OrgSwitcherComponent],
  host: { '(document:keydown.escape)': 'onEscape()' },
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayoutComponent {
  private readonly auth = inject(AuthService);
  private readonly context = inject(OrganizationContextService);
  private readonly organizations = inject(OrganizationStore);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly sidebarOpen = signal(true);
  private readonly menuToggle = viewChild.required<ElementRef<HTMLButtonElement>>('menuToggle');

  protected readonly isSidebarOpen = this.sidebarOpen.asReadonly();
  protected readonly user = this.auth.currentUser;
  protected readonly canSwitchOrganization = this.context.canSwitchOrganization;
  protected readonly navItems = computed(() => navItemsFor(this.auth.role()));

  protected readonly roleName = computed(() => {
    const role = this.auth.role();
    return role === null ? '' : roleLabel(role);
  });

  /** Shown in the navbar so the active tenant is always visible. */
  protected readonly organizationName = computed(() => {
    const id = this.context.activeOrganizationId();
    if (id === null) {
      return this.canSwitchOrganization() ? 'All organizations' : 'No organization';
    }
    return this.organizations.findById(id)?.name ?? 'Current organization';
  });

  constructor() {
    // The switcher and the navbar label both need the organization list.
    if (this.organizations.status() === 'idle') {
      this.organizations.loadAll();
    }
  }

  protected toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  protected closeSidebarOnMobile(): void {
    if (this.isMobile()) {
      this.sidebarOpen.set(false);
    }
  }

  /** On mobile the open sidebar is an overlay, so Escape dismisses it like a dialog. */
  protected onEscape(): void {
    if (!this.isMobile() || !this.sidebarOpen() || document.querySelector('[aria-modal="true"]')) {
      return;
    }
    this.sidebarOpen.set(false);
    this.menuToggle().nativeElement.focus();
  }

  private isMobile(): boolean {
    return window.matchMedia('(max-width: 768px)').matches;
  }

  protected logout(): void {
    this.auth
      .logout()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigate(['/login']),
        error: () => void this.router.navigate(['/login']),
      });
  }
}
