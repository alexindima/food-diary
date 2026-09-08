import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';

export const adminBugsRoutes: Routes = ['', ':id'].map(path => ({
    path,
    canActivate: [adminAuthGuard],
    loadComponent: async () => import('./pages/admin-bugs').then(module => module.AdminBugsPageComponent),
}));
