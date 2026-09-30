import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';

export const adminMealPlansRoutes: Routes = [
    {
        path: '',
        canActivate: [adminAuthGuard],
        loadComponent: async () => import('./pages/admin-meal-plans').then(module => module.AdminMealPlansComponent),
    },
];
