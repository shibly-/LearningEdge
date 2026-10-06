import type { AppEnvironment } from './environment.model';
import { UserRole } from '../app/core/models/user-role';
import { MOCK_ORG_A_ID } from '../app/core/models/mock-ids';

/**
 * Copy to environment.development.ts (gitignored) and adjust locally:
 *   npm run setup:env
 *
 * apiBaseUrl: https://localhost:7176 for `dotnet run`, https://localhost:5001 for docker compose.
 */
export const environment: AppEnvironment = {
  production: false,
  apiBaseUrl: 'https://localhost:7176',
  apiVersion: 'v1',
  useMockApi: true,
  useOidc: false,
  oidc: {
    authority: 'https://localhost:5003',
    clientId: 'learningedge-ui',
    scope: 'openid profile email org_id',
  },
  // userId must match a record in public/mock-data/tms-sample-data.json,
  // otherwise the signed-in identity owns no sample rows and the learner and
  // instructor screens render empty.
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
