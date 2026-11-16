import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { OrganizationContextService } from '../core/services/organization-context.service';
import { OrganizationRepository } from '../core/services/organization.repository';
import { OrganizationStore } from '../core/stores/organization.store';
import { describeError } from '../core/stores/async-collection.store';
import { ToastService } from '../core/services/toast.service';
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
  private readonly organizationRepository = inject(OrganizationRepository);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  private readonly sidebarOpen = signal(true);
  /** Name for a tenant user. The full organization list is system-admin only. */
  private readonly homeOrgName = signal<string | null>(null);
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
    if (this.auth.isPlatformUser()) {
      return this.organizations.findById(id)?.name ?? 'Current organization';
    }
    return this.homeOrgName() ?? 'Current organization';
  });

  constructor() {
    if (this.auth.isPlatformUser()) {
      if (this.organizations.status() === 'idle') {
        this.organizations.loadAll();
      }
      return;
    }

    const id = this.context.activeOrganizationId();
    if (id === null) {
      return;
    }

    // The public directory is id and name only, which is all the navbar needs.
    this.organizationRepository
      .directory()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (choices) => {
          this.homeOrgName.set(choices.find((choice) => choice.id === id)?.name ?? null);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
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

  /** Ends the Keycloak session too, otherwise check-sso would sign us straight back in. */
  protected logout(): void {
    void this.auth.signOut().catch(() => void this.router.navigate(['/login']));
  }
}
