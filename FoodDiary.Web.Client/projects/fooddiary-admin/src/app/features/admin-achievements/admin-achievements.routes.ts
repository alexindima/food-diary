import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';

export const adminAchievementsRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-achievements').then(module => module.AdminAchievementsComponent),
        canActivate: [adminAuthGuard],
    },
];
