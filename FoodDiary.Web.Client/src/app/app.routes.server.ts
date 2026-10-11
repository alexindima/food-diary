import { RenderMode, type ServerRoute } from '@angular/ssr';

import { localizedPublicPath, PUBLIC_MARKETING_PATHS } from './config/public-site-paths';

export const serverRoutes: ServerRoute[] = [
    ...Array.from(PUBLIC_MARKETING_PATHS).flatMap(path =>
        (['en', 'ru'] as const).map((locale): ServerRoute => ({
            path: localizedPublicPath(path, locale).slice(1),
            renderMode: RenderMode.Prerender,
        })),
    ),
    {
        path: 'verify-pending',
        renderMode: RenderMode.Client,
    },
    {
        path: 'verify-email',
        renderMode: RenderMode.Client,
    },
    {
        path: 'reset-password',
        renderMode: RenderMode.Client,
    },
    {
        path: 'products',
        renderMode: RenderMode.Client,
    },
    {
        path: 'meals',
        renderMode: RenderMode.Client,
    },
    {
        path: 'recipes',
        renderMode: RenderMode.Client,
    },
    {
        path: 'explore',
        renderMode: RenderMode.Server,
    },
    {
        path: 'explore/:id',
        renderMode: RenderMode.Server,
    },
    {
        path: 'shopping-lists',
        renderMode: RenderMode.Client,
    },
    {
        path: 'goals',
        renderMode: RenderMode.Client,
    },
    {
        path: 'statistics',
        renderMode: RenderMode.Client,
    },
    {
        path: 'weight-history',
        renderMode: RenderMode.Client,
    },
    {
        path: 'waist-history',
        renderMode: RenderMode.Client,
    },
    {
        path: 'cycle-tracking',
        renderMode: RenderMode.Client,
    },
    {
        path: 'meal-plans',
        renderMode: RenderMode.Client,
    },
    {
        path: 'weekly-check-in',
        renderMode: RenderMode.Client,
    },
    {
        path: 'lessons',
        renderMode: RenderMode.Client,
    },
    {
        path: 'gamification',
        renderMode: RenderMode.Client,
    },
    {
        path: 'fasting',
        renderMode: RenderMode.Client,
    },
    {
        path: 'premium',
        renderMode: RenderMode.Client,
    },
    {
        path: 'profile',
        renderMode: RenderMode.Client,
    },
    {
        path: 'dietologist',
        renderMode: RenderMode.Client,
    },
    {
        path: 'dietologist-invitations/:invitationId',
        renderMode: RenderMode.Client,
    },
    {
        path: 'recommendations',
        renderMode: RenderMode.Client,
    },
    {
        path: 'privacy-policy',
        renderMode: RenderMode.Prerender,
    },
    {
        path: '**',
        renderMode: RenderMode.Client,
    },
];
