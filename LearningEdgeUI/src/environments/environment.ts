import type { AppEnvironment } from './environment.model';

/**
 * Committed baseline. Never put credentials here.
 * `ng serve` / `ng build --configuration development` replace this file with
 * environment.development.ts (gitignored).
 */
export const environment: AppEnvironment = {
  production: true,
  apiBaseUrl: 'https://localhost:5001',
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
