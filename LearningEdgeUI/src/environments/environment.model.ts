import { UserRole } from '../app/core/models/user-role';

/**
 * A dev-only login. The password is checked in the browser.
 *
 * With `useMockApi: false` the login is matched to the API user with the same
 * `email`, and that record's id, role and organization are used; `userId` and
 * `organizationId` below then only matter for mock data. A SysAdmin login with
 * no matching API user still signs in, so an empty database can be seeded.
 */
export interface MockCredential {
  readonly username: string;
  readonly password: string;
  readonly role: UserRole;
  /** Mock data only. null for platform scope (SysAdmin). */
  readonly organizationId: string | null;
  /**
   * Mock data only: id of the matching record in
   * public/mock-data/tms-sample-data.json, so the learner and instructor
   * screens have sample rows to show.
   */
  readonly userId?: string;
  readonly displayName?: string;
  /** Required for every non-SysAdmin login when using the live API. */
  readonly email?: string;
}

export interface AppEnvironment {
  readonly production: boolean;
  /** Origin only. Path versioning is applied by core/http/api-paths.ts. */
  readonly apiBaseUrl: string;
  readonly apiVersion: string;
  /**
   * true: organizations, users, categories and trainings come from the sample
   * data. false: they come from the API. Assignments, messages and processing
   * are in-memory either way — the API has no endpoints for them yet.
   */
  readonly useMockApi: boolean;
  /** Swaps MockAuthProvider for OidcAuthProvider. */
  readonly useOidc: boolean;
  readonly oidc: {
    readonly authority: string;
    readonly clientId: string;
    readonly scope: string;
  };
  readonly mockCredentials: readonly MockCredential[];
  /**
   * GET /api/v1/user/{id} is limited to 5 requests per 10s and rejects with
   * HTTP 503 because RejectionStatusCode is not configured server-side.
   */
  readonly rateLimitRetry: {
    readonly maxAttempts: number;
    readonly baseDelayMs: number;
    readonly maxDelayMs: number;
  };
}
