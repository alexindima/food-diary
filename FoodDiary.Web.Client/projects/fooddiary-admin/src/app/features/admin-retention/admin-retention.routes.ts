import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';

export const adminRetentionRoutes: Routes = [
    {
        path: '',
        canActivate: [adminAuthGuard],
        loadComponent: async () => import('./pages/admin-retention').then(module => module.AdminRetentionComponent),
    },
];
