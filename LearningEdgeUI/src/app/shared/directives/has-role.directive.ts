import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import type { UserRole } from '../../core/models/user-role';

/**
 * Hides UI the current role may not use: `@if` on the template alternative.
 *
 *   <button *appHasRole="[UserRole.OrgAdmin]">Add user</button>
 *
 * This is presentation only — guards and the API remain the access boundary.
 */
@Directive({ selector: '[appHasRole]' })
export class HasRoleDirective {
  private readonly auth = inject(AuthService);
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);

  readonly appHasRole = input.required<readonly UserRole[]>();

  private rendered = false;

  constructor() {
    effect(() => {
      const allowed = this.auth.hasAnyRole(this.appHasRole());
      if (allowed && !this.rendered) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.rendered = true;
      } else if (!allowed && this.rendered) {
        this.viewContainer.clear();
        this.rendered = false;
      }
    });
  }
}
