import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
export const adminUsersRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-users').then(m => m.AdminUsersComponent),
        canActivate: [adminAuthGuard],
    },
    {
        path: 'login-activity',
        loadComponent: async () => import('./pages/admin-login-activity-page').then(m => m.AdminLoginActivityPageComponent),
        canActivate: [adminAuthGuard],
    },
    {
        path: 'impersonation-sessions',
        loadComponent: async () => import('./pages/admin-impersonation-sessions-page').then(m => m.AdminImpersonationSessionsPageComponent),
        canActivate: [adminAuthGuard],
    },
];
