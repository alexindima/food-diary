import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminDashboardFacade } from './lib/admin-dashboard.facade';

export const adminDashboardRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-dashboard').then(m => m.AdminDashboardComponent),
        canActivate: [adminAuthGuard],
        providers: [AdminDashboardFacade],
    },
];
