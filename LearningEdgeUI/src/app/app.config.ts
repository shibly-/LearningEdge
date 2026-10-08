import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';

import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { AUTH_PROVIDER } from './core/auth/auth-provider';
import { MockAuthProvider } from './core/auth/mock-auth.provider';
import { OidcAuthProvider } from './core/auth/oidc-auth.provider';
import { authInterceptor } from './core/auth/auth.interceptor';
import { retryInterceptor } from './core/http/retry.interceptor';
import {
  OrganizationRepository,
  HttpOrganizationRepository,
  MockOrganizationRepository,
} from './core/services/organization.repository';
import {
  UserRepository,
  HttpUserRepository,
  MockUserRepository,
} from './core/services/user.repository';
import {
  CategoryRepository,
  HttpCategoryRepository,
  MockCategoryRepository,
} from './core/services/category.repository';
import {
  TrainingRepository,
  HttpTrainingRepository,
  MockTrainingRepository,
} from './core/services/training.repository';
import {
  AssignmentRepository,
  MockAssignmentRepository,
} from './core/services/assignment.repository';
import { MessageRepository, MockMessageRepository } from './core/services/message.repository';
import {
  ProcessingRepository,
  MockProcessingRepository,
} from './core/services/processing.repository';
import { MockDataLoader } from './core/services/mock/mock-data.loader';

/**
 * Every repository is selected here by environment flag, so swapping mock for
 * live wiring never requires touching a feature (spec 3.7).
 *
 * Organizations, users, categories and trainings follow `useMockApi`.
 * Assignments, messages and processing have no backend endpoint yet, so they
 * always run in memory; their Http* placeholders are swapped in once the API
 * grows those routes.
 */
const apiBackedRepositories = environment.useMockApi
  ? [
      { provide: OrganizationRepository, useClass: MockOrganizationRepository },
      { provide: UserRepository, useClass: MockUserRepository },
      { provide: CategoryRepository, useClass: MockCategoryRepository },
      { provide: TrainingRepository, useClass: MockTrainingRepository },
    ]
  : [
      { provide: OrganizationRepository, useClass: HttpOrganizationRepository },
      { provide: UserRepository, useClass: HttpUserRepository },
      { provide: CategoryRepository, useClass: HttpCategoryRepository },
      { provide: TrainingRepository, useClass: HttpTrainingRepository },
    ];

const mockOnlyRepositories = [
  { provide: AssignmentRepository, useClass: MockAssignmentRepository },
  { provide: MessageRepository, useClass: MockMessageRepository },
  { provide: ProcessingRepository, useClass: MockProcessingRepository },
];

/**
 * Sample data is fetched before the first route renders, so no screen has to
 * cope with a half-seeded store. Needed in both modes: the mock-only features
 * read it even when the rest of the app talks to the API.
 */
const mockDataProviders = [provideAppInitializer(() => inject(MockDataLoader).load())];

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'top' }),
    ),
    // Functional interceptors only. retryInterceptor runs closest to the
    // backend so it can absorb 503s before anything else observes them.
    provideHttpClient(withInterceptors([authInterceptor, retryInterceptor])),
    {
      provide: AUTH_PROVIDER,
      useClass: environment.useOidc ? OidcAuthProvider : MockAuthProvider,
    },
    ...apiBackedRepositories,
    ...mockOnlyRepositories,
    ...mockDataProviders,
  ],
};
