/** GET /api/v1/organization/{organizationId}/category -> CategoryDTO[] */
export interface Category {
  readonly id: string;
  readonly organizationId: string;
  readonly name: string;
  readonly description: string;
  readonly isActive: boolean;
}

/** POST /api/v1/organization/{organizationId}/category -> 201 with the new GUID */
export interface CreateCategoryCommand {
  readonly organizationId: string;
  readonly name: string;
  readonly description: string;
  readonly isActive: boolean;
}

/** PUT /api/v1/organization/{organizationId}/category/{id} -> CategoryDTO */
export interface UpdateCategoryCommand extends CreateCategoryCommand {
  readonly id: string;
}

/** Mirrors the server-side validators. */
export const CATEGORY_LIMITS = { name: 100, description: 500 } as const;
