import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminBillingFacade } from './lib/admin-billing.facade';

export const adminBillingRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-billing').then(m => m.AdminBillingComponent),
        canActivate: [adminAuthGuard],
        providers: [AdminBillingFacade],
    },
];
