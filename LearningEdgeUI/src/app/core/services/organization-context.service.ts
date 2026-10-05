import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { TenantResetBus } from './tenant-reset-bus';

/** A SysAdmin's org-switcher choice, tied to the identity that made it. */
interface OrganizationSelection {
  readonly userId: string;
  readonly organizationId: string | null;
}

/**
 * Owns the active tenant. For tenant-scoped roles it is pinned to the user's own
 * organization and is read-only; only SysAdmin can switch (spec 4.3).
 *
 * The active id is derived, not assigned from an effect: tenant.guard.ts reads
 * it synchronously during navigation, which happens before effects flush.
 */
@Injectable({ providedIn: 'root' })
export class OrganizationContextService {
  private readonly auth = inject(AuthService);
  private readonly resetBus = inject(TenantResetBus);

  private readonly selection = signal<OrganizationSelection | null>(null);

  readonly activeOrganizationId = computed<string | null>(() => {
    const user = this.auth.currentUser();
    if (user === null) {
      return null;
    }
    if (!this.auth.isPlatformUser()) {
      return user.organizationId;
    }
    // A selection made by a previous session must not carry over.
    const selection = this.selection();
    return selection !== null && selection.userId === user.id ? selection.organizationId : null;
  });

  readonly canSwitchOrganization = computed(() => this.auth.isPlatformUser());
  readonly hasOrganization = computed(() => this.activeOrganizationId() !== null);

  /**
   * SysAdmin only. Clears every tenant-scoped store so stale data from the
   * previous organization cannot leak into the new one.
   */
  switchOrganization(organizationId: string | null): void {
    const user = this.auth.currentUser();
    if (user === null || !this.canSwitchOrganization()) {
      return;
    }
    if (this.activeOrganizationId() === organizationId) {
      return;
    }
    this.selection.set({ userId: user.id, organizationId });
    this.resetBus.resetAll();
  }

  /** Throws rather than silently querying the wrong tenant. */
  requireOrganizationId(): string {
    const id = this.activeOrganizationId();
    if (id === null) {
      throw new Error('No active organization. Select one before loading tenant-scoped data.');
    }
    return id;
  }
}
