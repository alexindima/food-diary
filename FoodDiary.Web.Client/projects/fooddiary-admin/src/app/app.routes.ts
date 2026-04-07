import { Routes } from '@angular/router';
import { adminAiUsageRoutes } from './features/admin-ai-usage/admin-ai-usage.routes';
import { adminDashboardRoutes } from './features/admin-dashboard/admin-dashboard.routes';
import { adminEmailTemplatesRoutes } from './features/admin-email-templates/admin-email-templates.routes';
import { adminLessonsRoutes } from './features/admin-lessons/admin-lessons.routes';
import { adminModerationRoutes } from './features/admin-moderation/admin-moderation.routes';
import { adminPublicRoutes } from './features/admin-public/admin-public.routes';
import { adminUsersRoutes } from './features/admin-users/admin-users.routes';

export const routes: Routes = [
    {
        path: '',
        children: adminDashboardRoutes,
    },
    {
        path: 'users',
        children: adminUsersRoutes,
    },
    {
        path: 'ai-usage',
        children: adminAiUsageRoutes,
    },
    {
        path: 'email-templates',
        children: adminEmailTemplatesRoutes,
    },
    {
        path: 'lessons',
        children: adminLessonsRoutes,
    },
    {
        path: 'moderation',
        children: adminModerationRoutes,
    },
    {
        path: '',
        children: adminPublicRoutes,
    },
    {
        path: '**',
        redirectTo: '',
    },
];
