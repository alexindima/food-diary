import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
export const adminAcquisitionRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-acquisition').then(m => m.AdminAcquisitionComponent),
        canActivate: [adminAuthGuard],
    },
];
