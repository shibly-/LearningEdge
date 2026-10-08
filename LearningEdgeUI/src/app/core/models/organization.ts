/**
 * GET /api/v1/organization/{id} -> OrganizationDTO
 *
 * The Organization entity has ONLY Name and Description. Do not add address,
 * phone, or logo fields — the API ignores them.
 */
export interface OrganizationDto {
  readonly id: string;
  readonly name: string;
  readonly description: string;
}

/** POST /api/v1/organization -> 201 with the new GUID */
export interface CreateOrganizationCommand {
  readonly name: string;
  readonly description: string;
}

/** PUT /api/v1/organization/{id} -> OrganizationDTO */
export type UpdateOrganizationCommand = CreateOrganizationCommand;

/** Mirrors the server-side validators. */
export const ORGANIZATION_LIMITS = { name: 100, description: 500 } as const;
