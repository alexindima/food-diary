import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
export const adminModerationRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-moderation').then(m => m.AdminModerationComponent),
        canActivate: [adminAuthGuard],
    },
];
