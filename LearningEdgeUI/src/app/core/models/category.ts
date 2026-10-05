// MOCK: no backend endpoint. Frontend-owned contract (spec 3.7).

export interface Category {
  readonly id: string;
  readonly organizationId: string;
  readonly name: string;
  readonly description: string;
  readonly trainingCount: number;
  readonly createdAt: string;
}

export interface CreateCategoryCommand {
  readonly organizationId: string;
  readonly name: string;
  readonly description: string;
}
