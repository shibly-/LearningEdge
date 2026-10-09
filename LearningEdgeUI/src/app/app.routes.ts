import type { Routes } from '@angular/router';
import { anonymousOnlyGuard, authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { tenantGuard } from './core/auth/tenant.guard';
import { UserRole } from './core/models/user-role';

/**
 * Route matrix from spec section 6. Every feature is lazy-loaded; roles are
 * declared in `data` and enforced by roleGuard.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },

  {
    path: 'login',
    canActivate: [anonymousOnlyGuard],
    title: 'Sign in · LearningEdge',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [anonymousOnlyGuard],
    title: 'Request access · LearningEdge',
    loadComponent: () =>
      import('./features/auth/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    canActivate: [anonymousOnlyGuard],
    title: 'Reset password · LearningEdge',
    loadComponent: () =>
      import('./features/auth/forgot-password.component').then((m) => m.ForgotPasswordComponent),
  },

  {
    path: '',
    loadComponent: () =>
      import('./layout/main-layout.component').then((m) => m.MainLayoutComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/dashboard/dashboard-redirect.component').then(
            (m) => m.DashboardRedirectComponent,
          ),
      },
      {
        path: 'dashboard/admin',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.OrgAdmin] },
        title: 'Dashboard · LearningEdge',
        loadComponent: () =>
          import('./features/dashboard/admin-dashboard.component').then(
            (m) => m.AdminDashboardComponent,
          ),
      },
      {
        path: 'dashboard/staff',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.Instructor] },
        title: 'Dashboard · LearningEdge',
        loadComponent: () =>
          import('./features/dashboard/staff-dashboard.component').then(
            (m) => m.StaffDashboardComponent,
          ),
      },

      // Platform scope — SysAdmin only, so no tenant guard applies.
      {
        path: 'platform',
        pathMatch: 'full',
        canActivate: [roleGuard],
        data: { roles: [UserRole.SysAdmin] },
        title: 'Platform Overview · LearningEdge',
        loadComponent: () =>
          import('./features/super-admin/platform-dashboard.component').then(
            (m) => m.PlatformDashboardComponent,
          ),
      },
      {
        path: 'platform/organizations',
        pathMatch: 'full',
        canActivate: [roleGuard],
        data: { roles: [UserRole.SysAdmin] },
        title: 'Organizations · LearningEdge',
        loadComponent: () =>
          import('./features/organizations/organization-list.component').then(
            (m) => m.OrganizationListComponent,
          ),
      },
      {
        path: 'platform/organizations/:organizationId',
        canActivate: [roleGuard],
        data: { roles: [UserRole.SysAdmin] },
        title: 'Organization · LearningEdge',
        loadComponent: () =>
          import('./features/organizations/organization-detail.component').then(
            (m) => m.OrganizationDetailComponent,
          ),
      },
      {
        path: 'platform/org-admins',
        canActivate: [roleGuard],
        data: { roles: [UserRole.SysAdmin] },
        title: 'Organization admins · LearningEdge',
        loadComponent: () =>
          import('./features/super-admin/org-admin-list.component').then(
            (m) => m.OrgAdminListComponent,
          ),
      },
      {
        path: 'platform/org-admins/new',
        canActivate: [roleGuard],
        data: { roles: [UserRole.SysAdmin] },
        title: 'Add organization admin · LearningEdge',
        loadComponent: () =>
          import('./features/super-admin/org-admin-creator.component').then(
            (m) => m.OrgAdminCreatorComponent,
          ),
      },
      {
        path: 'platform/users',
        canActivate: [roleGuard],
        data: { roles: [UserRole.SysAdmin] },
        title: 'Users & Staff · LearningEdge',
        loadComponent: () =>
          import('./features/super-admin/platform-user-list.component').then(
            (m) => m.PlatformUserListComponent,
          ),
      },

      // Tenant scope.
      {
        path: 'users',
        pathMatch: 'full',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.OrgAdmin] },
        title: 'Users & Staff · LearningEdge',
        loadComponent: () =>
          import('./features/users/user-list.component').then((m) => m.UserListComponent),
      },
      {
        path: 'users/:userId',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.OrgAdmin] },
        title: 'User · LearningEdge',
        loadComponent: () =>
          import('./features/users/user-detail.component').then((m) => m.UserDetailComponent),
      },
      {
        path: 'categories',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.OrgAdmin, UserRole.Instructor] },
        title: 'Categories · LearningEdge',
        loadComponent: () =>
          import('./features/categories/category-list.component').then(
            (m) => m.CategoryListComponent,
          ),
      },
      {
        path: 'trainings',
        pathMatch: 'full',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.OrgAdmin, UserRole.Instructor] },
        title: 'Trainings · LearningEdge',
        loadComponent: () =>
          import('./features/trainings/training-list.component').then(
            (m) => m.TrainingListComponent,
          ),
      },
      {
        path: 'trainings/assign',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.OrgAdmin, UserRole.Instructor] },
        title: 'Assign training · LearningEdge',
        loadComponent: () =>
          import('./features/trainings/training-assign.component').then(
            (m) => m.TrainingAssignComponent,
          ),
      },
      {
        path: 'trainings/category/:categoryId',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.OrgAdmin, UserRole.Instructor] },
        title: 'Category · LearningEdge',
        loadComponent: () =>
          import('./features/trainings/category-view.component').then(
            (m) => m.CategoryViewComponent,
          ),
      },
      {
        path: 'processing',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.OrgAdmin, UserRole.Instructor] },
        title: 'Material Processing · LearningEdge',
        loadComponent: () =>
          import('./features/processing/material-upload.component').then(
            (m) => m.MaterialUploadComponent,
          ),
      },
      {
        path: 'staff-ops',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.Instructor] },
        title: 'Trainee operations · LearningEdge',
        loadComponent: () =>
          import('./features/staff-ops/trainee-ops.component').then((m) => m.TraineeOpsComponent),
      },
      {
        path: 'portal',
        canActivate: [roleGuard, tenantGuard],
        data: { roles: [UserRole.Learner] },
        title: 'My training · LearningEdge',
        loadComponent: () =>
          import('./features/user-portal/user-portal.component').then((m) => m.UserPortalComponent),
      },

      {
        path: '**',
        loadComponent: () =>
          import('./shared/components/not-found.component').then((m) => m.NotFoundComponent),
      },
    ],
  },
];
