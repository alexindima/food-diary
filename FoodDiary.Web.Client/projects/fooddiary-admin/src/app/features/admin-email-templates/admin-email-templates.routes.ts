import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
export const adminEmailTemplatesRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-email-templates').then(m => m.AdminEmailTemplatesComponent),
        canActivate: [adminAuthGuard],
    },
];
