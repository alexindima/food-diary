import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
export const adminLessonsRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-lessons').then(m => m.AdminLessonsComponent),
        canActivate: [adminAuthGuard],
    },
];
