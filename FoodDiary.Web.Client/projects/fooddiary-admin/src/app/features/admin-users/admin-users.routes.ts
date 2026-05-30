import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminImpersonationSessionsPageComponent } from './pages/admin-impersonation-sessions-page';
import { AdminLoginActivityPageComponent } from './pages/admin-login-activity-page';
import { AdminUsersComponent } from './pages/admin-users';

export const adminUsersRoutes: Routes = [
    {
        path: '',
        component: AdminUsersComponent,
        canActivate: [adminAuthGuard],
    },
    {
        path: 'login-activity',
        component: AdminLoginActivityPageComponent,
        canActivate: [adminAuthGuard],
    },
    {
        path: 'impersonation-sessions',
        component: AdminImpersonationSessionsPageComponent,
        canActivate: [adminAuthGuard],
    },
];
