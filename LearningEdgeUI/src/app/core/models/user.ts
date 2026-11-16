import type { UserRole } from './user-role';

/** Mirrors LearningEdge.Domain.Common.Enums.UserActivationStatus. Wire format is a number. */
export enum UserActivationStatus {
  Pending = 1,
  Active = 2,
}

/** GET /api/v1/user/{id} -> UserDTO */
export interface UserDto {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly role: UserRole;
  readonly organizationId: string;
  readonly activationStatus: UserActivationStatus;
}

/** POST /api/v1/user/register. Role is always Learner and the account stays pending. */
export interface RegisterLearnerCommand {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly password: string;
  readonly organizationId: string;
}

/** POST /api/v1/user -> 201 with the new GUID. Staff and trainees only. */
export interface CreateUserCommand {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly role: UserRole;
  readonly organizationId: string;
}

/**
 * POST /api/v1/user/organization-admin -> 201 with the new GUID.
 * System admin only. The password is the Keycloak sign-in password.
 */
export interface CreateOrganizationAdminCommand {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly password: string;
  readonly organizationId: string;
}

/** PUT /api/v1/user/{id} -> UserDTO */
export type UpdateUserCommand = CreateUserCommand;

/** Mirrors the server-side validators. */
export const USER_LIMITS = { firstName: 50, lastName: 50, email: 100 } as const;

export function fullName(user: Pick<UserDto, 'firstName' | 'lastName'>): string {
  return `${user.firstName} ${user.lastName}`.trim();
}

/** Pending is named in text so status is not conveyed by colour alone. */
export function activationLabel(status: UserActivationStatus | undefined): string {
  return status === UserActivationStatus.Pending ? 'Pending approval' : 'Active';
}

export function isPendingActivation(status: UserActivationStatus | undefined): boolean {
  return status === UserActivationStatus.Pending;
}
