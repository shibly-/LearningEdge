import type { AppEnvironment } from './environment.model';
import { UserRole } from '../app/core/models/user-role';
import { MOCK_ORG_A_ID } from '../app/core/models/mock-ids';

/**
 * Used by the `ui` service in docker-compose.yml. Never add a real credential
 * here â€” the mock logins below are the placeholder ones already published in
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
  // Live API through the nginx proxy. Logins are matched to API users by email;
  // on a fresh database sign in as superadmin and create the users first.
  // Set to true to demo on sample data instead.
  useMockApi: false,
  useOidc: false,
  oidc: {
    authority: '',
    clientId: 'learningedge-ui',
    scope: 'openid profile email org_id',
  },
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
