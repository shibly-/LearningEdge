import type { AppEnvironment } from './environment.model';

/**
 * Copy to environment.development.ts (gitignored) and adjust locally:
 *   npm run setup:env
 *
 * apiBaseUrl: https://localhost:7176 for `dotnet run`, https://localhost:5001 for docker compose.
 *
 * Sign-in goes through Keycloak, so there are no credentials to configure here.
 * The realm must trust http://localhost:4200 as a redirect origin, and the API's
 * Cors:Origin setting must name the same origin.
 */
export const environment: AppEnvironment = {
  production: false,
  apiBaseUrl: 'https://localhost:7176',
  apiVersion: 'v1',
  useMockApi: false,
  keycloak: {
    url: 'https://localhost:8443',
    realm: 'learningedge',
    clientId: 'le-web-ui',
    sessionTimeoutMs: 1_800_000,
  },
  rateLimitRetry: {
    maxAttempts: 4,
    baseDelayMs: 500,
    maxDelayMs: 8000,
  },
};
