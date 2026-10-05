/**
 * GET /api/v1/organization/{id} -> ApiResult<OrganizationDTO>
 *
 * The Organization entity has ONLY Name and Description. Do not add address,
 * phone, or logo fields — the API ignores them.
 */
export interface OrganizationDto {
  readonly id: string;
  readonly name: string;
  readonly description: string;
}

/** POST /api/v1/organization -> ApiResult<string> (the new GUID) */
export interface CreateOrganizationCommand {
  readonly name: string;
  readonly description: string;
}
