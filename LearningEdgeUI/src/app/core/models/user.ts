import type { UserRole } from './user-role';

/** GET /api/v1/user/{id} -> ApiResult<UserDTO> */
export interface UserDto {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  /** Plain string server-side; the Email value object is not wired to User. */
  readonly email: string;
  readonly role: UserRole;
  readonly organizationId: string;
}

/** POST /api/v1/user -> ApiResult<string> (the new GUID, not the DTO) */
export interface CreateUserCommand {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly role: UserRole;
  readonly organizationId: string;
}

export function fullName(user: Pick<UserDto, 'firstName' | 'lastName'>): string {
  return `${user.firstName} ${user.lastName}`.trim();
}
