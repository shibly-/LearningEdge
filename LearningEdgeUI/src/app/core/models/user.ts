import type { UserRole } from './user-role';

/** GET /api/v1/user/{id} -> UserDTO */
export interface UserDto {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly role: UserRole;
  readonly organizationId: string;
}

/** POST /api/v1/user -> 201 with the new GUID */
export interface CreateUserCommand {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly role: UserRole;
  readonly organizationId: string;
}

/** PUT /api/v1/user/{id} -> UserDTO */
export type UpdateUserCommand = CreateUserCommand;

/** Mirrors the server-side validators. */
export const USER_LIMITS = { firstName: 50, lastName: 50, email: 100 } as const;

export function fullName(user: Pick<UserDto, 'firstName' | 'lastName'>): string {
  return `${user.firstName} ${user.lastName}`.trim();
}
