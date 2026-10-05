import { Pipe } from '@angular/core';
import type { PipeTransform } from '@angular/core';
import { roleLabel } from '../../core/models/user-role';
import type { UserRole } from '../../core/models/user-role';

/** Numeric role to the display vocabulary (Trainee, Staff, Admin, Super Admin). */
@Pipe({ name: 'roleLabel' })
export class RoleLabelPipe implements PipeTransform {
  transform(role: UserRole | null | undefined): string {
    return role === null || role === undefined ? '—' : roleLabel(role);
  }
}
