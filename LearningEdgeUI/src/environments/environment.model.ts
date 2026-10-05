import { UserRole } from '../app/core/models/user-role';

/**
 * A dev-only login. `organizationId` is null for platform scope (SysAdmin).
 */
export interface MockCredential {
  readonly username: string;
  readonly password: string;
  readonly role: UserRole;
  readonly organizationId: string | null;
  /**
   * Id of the matching record in public/mock-data/tms-sample-data.json. Without
   * it the session has an identity no sample row points at, so the learner and
   * instructor screens would render empty.
   */
  readonly userId?: string;
  readonly displayName?: string;
  readonly email?: string;
}

export interface AppEnvironment {
  readonly production: boolean;
  /** Origin only. Path versioning is applied by core/http/api-paths.ts. */
  readonly apiBaseUrl: string;
  readonly apiVersion: string;
  /** Serves every repository from an in-memory adapter. */
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
