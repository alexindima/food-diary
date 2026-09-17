import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';

export const adminDailyAdvicesRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-daily-advices').then(module => module.AdminDailyAdvicesComponent),
        canActivate: [adminAuthGuard],
    },
];
