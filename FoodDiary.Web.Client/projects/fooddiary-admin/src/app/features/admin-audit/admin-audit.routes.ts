import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';

export const adminAuditRoutes: Routes = [
    {
        path: '',
        canActivate: [adminAuthGuard],
        loadComponent: async () => import('./pages/admin-audit').then(module => module.AdminAuditPageComponent),
    },
];
