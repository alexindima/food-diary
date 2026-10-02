import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';

export const adminCatalogRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-catalog').then(module => module.AdminCatalogComponent),
        canActivate: [adminAuthGuard],
    },
];
