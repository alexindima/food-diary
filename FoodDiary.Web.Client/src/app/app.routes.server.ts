import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
    {
        path: '',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'food-diary',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'calorie-counter',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'meal-planner',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'macro-tracker',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'intermittent-fasting',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'meal-tracker',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'weight-loss-app',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'dietologist-collaboration',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'nutrition-planner',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'weight-tracker',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'body-progress-tracker',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'shopping-list-for-meal-planning',
        renderMode: RenderMode.Prerender,
    },
    {
        path: 'auth',
        renderMode: RenderMode.Client,
    },
    {
        path: 'auth/:mode',
        renderMode: RenderMode.Client,
    },
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
        renderMode: RenderMode.Client,
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
        path: 'privacy-policy',
        renderMode: RenderMode.Prerender,
    },
    {
        path: '**',
        renderMode: RenderMode.Client,
    },
];
