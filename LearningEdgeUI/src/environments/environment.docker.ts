import type { AppEnvironment } from './environment.model';
import { environment as sampleEnvironment } from './environment.development.example';

/**
 * Used by the `ui` service in docker-compose.yml. Never add a real credential
 * here — the mock logins below are the placeholder ones already published in
 * environment.development.example.ts, reused so the container is demoable.
 *
 * apiBaseUrl is empty on purpose: requests go to the UI's own origin and nginx
 * forwards /api/ to the api container over appnet. The browser runs on the
 * host, so it could not resolve the `api` hostname itself.
 */
export const environment: AppEnvironment = {
  production: true,
  apiBaseUrl: '',
  apiVersion: 'v1',
  // The API registers no authentication scheme and has no list endpoints, so
  // the container ships demoable on sample data. Flip to false to exercise the
  // two live endpoints through the nginx proxy.
  useMockApi: true,
  useOidc: false,
  oidc: {
    authority: '',
    clientId: 'learningedge-ui',
    scope: 'openid profile email org_id',
  },
  //mockCredentials: sampleEnvironment.mockCredentials,
  mockCredentials: [
    {
      username: 'superadmin',
      password: 'Dev@12345',
      role: UserRole.SysAdmin,
      organizationId: null,
      displayName: 'Platform Operator',
    },
    {
      username: 'testadmin',
      password: 'Dev@12345',
      role: UserRole.OrgAdmin,
      organizationId: MOCK_ORG_A_ID,
      userId: 'user-a1',
      displayName: 'Ayesha Rahman',
      email: 'ayesha.rahman@northwind.example.com',
    },
    {
      username: 'staff',
      password: 'Dev@12345',
      role: UserRole.Instructor,
      organizationId: MOCK_ORG_A_ID,
      userId: 'user-a2',
      displayName: 'Daniel Okafor',
      email: 'daniel.okafor@northwind.example.com',
    },
    {
      username: 'trainee',
      password: 'Dev@12345',
      role: UserRole.Learner,
      organizationId: MOCK_ORG_A_ID,
      userId: 'user-a4',
      displayName: 'Tomas Varga',
      email: 'tomas.varga@northwind.example.com',
    },
  ],
  rateLimitRetry: {
    maxAttempts: 4,
    baseDelayMs: 500,
    maxDelayMs: 8000,
  },
};
