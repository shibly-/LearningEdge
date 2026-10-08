import { ChangeDetectionStrategy, Component } from '@angular/core';
import { UserRole } from '../../core/models/user-role';
import {
  PlatformUserDirectoryComponent,
  type RoleScope,
} from './platform-user-directory.component';

/** Staff and trainees of every organization; admins have their own page. */
@Component({
  selector: 'app-platform-user-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PlatformUserDirectoryComponent],
  template: `
    <app-platform-user-directory
      heading="Users & staff"
      subtitle="Staff and trainees across all organizations."
      [scopes]="scopes"
    />
  `,
})
export class PlatformUserListComponent {
  protected readonly scopes: readonly RoleScope[] = [
    { id: 'all', label: 'Everyone', roles: [UserRole.Instructor, UserRole.Learner] },
    { id: 'staff', label: 'Staff', roles: [UserRole.Instructor] },
    { id: 'trainees', label: 'Trainees', roles: [UserRole.Learner] },
  ];
}
