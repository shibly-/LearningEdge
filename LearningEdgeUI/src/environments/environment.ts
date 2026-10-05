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
  useMockApi: true,
  useOidc: false,
  oidc: {
    authority: '',
    clientId: 'learningedge-ui',
    scope: 'openid profile email org_id',
  },
  mockCredentials: [],
  rateLimitRetry: {
    maxAttempts: 4,
    baseDelayMs: 500,
    maxDelayMs: 8000,
  },
};
