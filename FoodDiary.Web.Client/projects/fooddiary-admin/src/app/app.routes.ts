import type { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        loadChildren: async () => import('./features/admin-dashboard/admin-dashboard.routes').then(m => m.adminDashboardRoutes),
    },
    {
        path: 'users',
        loadChildren: async () => import('./features/admin-users/admin-users.routes').then(m => m.adminUsersRoutes),
    },
    {
        path: 'ai-usage',
        loadChildren: async () => import('./features/admin-ai-usage/admin-ai-usage.routes').then(m => m.adminAiUsageRoutes),
    },
    {
        path: 'acquisition',
        loadChildren: async () => import('./features/admin-acquisition/admin-acquisition.routes').then(m => m.adminAcquisitionRoutes),
    },
    {
        path: 'billing',
        loadChildren: async () => import('./features/admin-billing/admin-billing.routes').then(m => m.adminBillingRoutes),
    },
    {
        path: 'email-templates',
        loadChildren: async () =>
            import('./features/admin-email-templates/admin-email-templates.routes').then(m => m.adminEmailTemplatesRoutes),
    },
    {
        path: 'mail-inbox',
        loadChildren: async () => import('./features/admin-mail-inbox/admin-mail-inbox.routes').then(m => m.adminMailInboxRoutes),
    },
    {
        path: 'lessons',
        loadChildren: async () => import('./features/admin-lessons/admin-lessons.routes').then(m => m.adminLessonsRoutes),
    },
    {
        path: 'moderation',
        loadChildren: async () => import('./features/admin-moderation/admin-moderation.routes').then(m => m.adminModerationRoutes),
    },
    {
        path: 'unauthorized',
        loadComponent: async () => import('./features/admin-public/pages/unauthorized').then(m => m.UnauthorizedComponent),
    },
    {
        path: '**',
        redirectTo: '',
    },
];
