/**
 * GET /api/v1/platform/snapshot. Totals only.
 * Trainings use the domain flag: active or inactive.
 */
export interface PlatformSnapshot {
  readonly organizationsActive: number;
  readonly organizationsInactive: number;
  readonly learnersActive: number;
  readonly learnersPending: number;
  readonly learnersInactive: number;
  readonly trainingsActive: number;
  readonly trainingsInactive: number;
}
