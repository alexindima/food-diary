import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
export const adminMailInboxRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-mail-inbox').then(m => m.AdminMailInboxComponent),
        canActivate: [adminAuthGuard],
    },
];
