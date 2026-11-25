import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
export const adminMailInboxRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-mail-inbox').then(m => m.AdminMailInboxComponent),
        data: { feedbackMailbox: false },
        canActivate: [adminAuthGuard],
    },
];

export const adminFeedbackRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-mail-inbox').then(m => m.AdminMailInboxComponent),
        data: { feedbackMailbox: true },
        canActivate: [adminAuthGuard],
    },
];

export const adminDmarcRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-dmarc').then(m => m.AdminDmarcComponent),
        canActivate: [adminAuthGuard],
    },
];
