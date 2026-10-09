import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UserRole } from '../../core/models/user-role';
import {
  PlatformUserDirectoryComponent,
  type RoleScope,
} from './platform-user-directory.component';

@Component({
  selector: 'app-org-admin-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PlatformUserDirectoryComponent],
  template: `
    <app-platform-user-directory
      heading="Organization Admins"
      subtitle="Administrators of every organization on the platform."
      emptyMessage="No organization admins yet. Add one to let a tenant manage its own users."
      [scopes]="scopes"
    >
      <a class="le-btn" routerLink="/platform/org-admins/new">Add organization admin</a>
    </app-platform-user-directory>
  `,
})
export class OrgAdminListComponent {
  protected readonly scopes: readonly RoleScope[] = [
    { id: 'admins', label: 'Admins', roles: [UserRole.OrgAdmin] },
  ];
}
