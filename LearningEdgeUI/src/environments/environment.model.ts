/**
 * Keycloak is the only sign-in path. The realm and both client ids are
 * configured server-side in LearningEdge.Api's `Keycloak` settings section;
 * the values here must agree with them.
 */
export interface KeycloakSettings {
  /** Keycloak origin, no trailing slash and no /realms segment. */
  readonly url: string;
  readonly realm: string;
  /**
   * The public browser client. Note this is *not* the audience the API
   * validates: tokens issued to it must carry the API's client id in `aud`,
   * which an audience mapper on the realm supplies.
   */
  readonly clientId: string;
  /**
   * Inactivity window. When the access token expires the session is refreshed
   * if the user interacted within this window, and ended otherwise.
   */
  readonly sessionTimeoutMs: number;
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
  readonly keycloak: KeycloakSettings;
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
