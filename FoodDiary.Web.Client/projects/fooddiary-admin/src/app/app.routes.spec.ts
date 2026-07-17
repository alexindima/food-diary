import type { Route, Routes } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { routes } from './app.routes';
import { adminAcquisitionRoutes } from './features/admin-acquisition/admin-acquisition.routes';
import { adminAiUsageRoutes } from './features/admin-ai-usage/admin-ai-usage.routes';
import { adminBillingRoutes } from './features/admin-billing/admin-billing.routes';
import { adminDashboardRoutes } from './features/admin-dashboard/admin-dashboard.routes';
import { adminEmailTemplatesRoutes } from './features/admin-email-templates/admin-email-templates.routes';
import { adminLessonsRoutes } from './features/admin-lessons/admin-lessons.routes';
import { adminMailInboxRoutes } from './features/admin-mail-inbox/admin-mail-inbox.routes';
import { adminModerationRoutes } from './features/admin-moderation/admin-moderation.routes';
import { adminUsersRoutes } from './features/admin-users/admin-users.routes';

const protectedFeatureRoutes: Routes[] = [
    adminDashboardRoutes,
    adminUsersRoutes,
    adminAiUsageRoutes,
    adminAcquisitionRoutes,
    adminBillingRoutes,
    adminEmailTemplatesRoutes,
    adminMailInboxRoutes,
    adminLessonsRoutes,
    adminModerationRoutes,
];

describe('admin routes', () => {
    it('loads every feature boundary lazily', () => {
        const featureBoundaries = routes.filter(route => route.path !== 'unauthorized' && route.path !== '**');

        expect(featureBoundaries).toHaveLength(protectedFeatureRoutes.length);
        for (const route of featureBoundaries) {
            expect(route.loadChildren).toBeTypeOf('function');
            expect(route.children).toBeUndefined();
            expect(route.component).toBeUndefined();
        }
    });

    it('loads every feature page lazily and protects it with the admin guard', () => {
        for (const featureRoutes of protectedFeatureRoutes) {
            for (const route of featureRoutes) {
                expect(route.loadComponent).toBeTypeOf('function');
                expect(route.component).toBeUndefined();
                expect(route.canActivate).toHaveLength(1);
            }
        }
    });

    it('resolves every lazy feature route definition', async () => {
        const featureBoundaries = routes.filter(
            (route): route is Route & { loadChildren: NonNullable<Route['loadChildren']> } =>
                route.path !== 'unauthorized' && route.path !== '**' && route.loadChildren !== undefined,
        );

        const loadedRoutes = await Promise.all(featureBoundaries.map(async route => route.loadChildren()));

        expect(loadedRoutes).toEqual(protectedFeatureRoutes);
    });

    it('keeps the public unauthorized page lazy and before the wildcard', () => {
        const unauthorizedIndex = routes.findIndex(route => route.path === 'unauthorized');
        const wildcardIndex = routes.findIndex(route => route.path === '**');
        const unauthorizedRoute = routes[unauthorizedIndex];

        expect(unauthorizedRoute.loadComponent).toBeTypeOf('function');
        expect(unauthorizedRoute.component).toBeUndefined();
        expect(unauthorizedIndex).toBeLessThan(wildcardIndex);
    });
});
