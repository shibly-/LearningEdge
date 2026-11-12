import type { AppEnvironment } from './environment.model';

/**
 * Used by the `ui` service in docker-compose.yml.
 *
 * apiBaseUrl is empty on purpose: requests go to the UI's own origin and nginx
 * forwards /api/ to the api container over appnet. The browser runs on the
 * host, so it could not resolve the `api` hostname itself.
 *
 * The Keycloak url, by contrast, must be host-reachable: the browser is
 * redirected to it, so the compose-internal hostname would not resolve.
 */
export const environment: AppEnvironment = {
  production: true,
  apiBaseUrl: '',
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
