import { Injectable } from '@angular/core';

/**
 * Tenant-scoped stores register a reset callback here. Switching organization
 * fires all of them so no cross-tenant data survives in memory (spec 4.3).
 */
@Injectable({ providedIn: 'root' })
export class TenantResetBus {
  private readonly handlers = new Set<() => void>();

  register(handler: () => void): void {
    this.handlers.add(handler);
  }

  resetAll(): void {
    for (const handler of this.handlers) {
      handler();
    }
  }
}
