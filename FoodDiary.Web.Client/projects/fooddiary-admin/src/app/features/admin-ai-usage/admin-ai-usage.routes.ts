import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
export const adminAiUsageRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-ai-usage').then(m => m.AdminAiUsageComponent),
        canActivate: [adminAuthGuard],
    },
];
