/**
 * GET /api/v1/organization/{id} -> OrganizationDTO
 *
 * The Organization entity has name, description, and whether it can sign users in.
 * Do not add address, phone, or logo fields — the API ignores them.
 */
export interface OrganizationDto {
  readonly id: string;
  readonly name: string;
  readonly description: string;
  readonly isActive: boolean;
}

/** POST /api/v1/organization -> 201 with the new GUID */
export interface CreateOrganizationCommand {
  readonly name: string;
  readonly description: string;
}

/** PUT /api/v1/organization/{id} -> OrganizationDTO */
export type UpdateOrganizationCommand = CreateOrganizationCommand;

/** PUT /api/v1/organization/{id}/active -> OrganizationDTO */
export interface SetOrganizationActiveCommand {
  readonly id: string;
  readonly isActive: boolean;
}

/** GET /api/v1/organization/directory — public, name only. */
export interface OrganizationChoice {
  readonly id: string;
  readonly name: string;
}

/** Mirrors the server-side validators. */
export const ORGANIZATION_LIMITS = { name: 100, description: 500 } as const;
