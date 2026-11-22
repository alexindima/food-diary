import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';

export const adminModerationRoutes: Routes = [
    {
        path: 'recipes',
        loadComponent: async () => import('./pages/admin-recipe-moderation').then(m => m.AdminRecipeModerationComponent),
        canActivate: [adminAuthGuard],
    },
    {
        path: 'rules',
        loadComponent: async () => import('./pages/admin-moderation-rules').then(m => m.AdminModerationRulesComponent),
        canActivate: [adminAuthGuard],
    },
    {
        path: '',
        pathMatch: 'full',
        loadComponent: async () => import('./pages/admin-moderation').then(m => m.AdminModerationComponent),
        canActivate: [adminAuthGuard],
    },
];
