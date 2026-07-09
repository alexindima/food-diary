import type { Type } from '@angular/core';
import type { Route, Routes } from '@angular/router';

import { PUBLIC_SEO_LANDING_ROUTES, type PublicSeoLandingRouteConfig } from './config/public-seo-landing-routes.config';
import { authRoutes } from './features/auth/auth.routes';
import { authGuard } from './guards/auth.guard';
import { dietologistGuard } from './guards/dietologist.guard';
import { loggedInGuard } from './guards/logged-in.guard';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { mobileShellStartGuard } from './shared/platform/mobile-shell-start.guard';

const publicSeoLanding = ({
    path,
    titleKey,
    descriptionKey,
    baseKey,
    featureKeys,
    stepKeys,
    faqKeys,
    relatedPaths,
}: PublicSeoLandingRouteConfig): Route => ({
    path,
    loadComponent: async (): Promise<Type<unknown>> =>
        import('./features/public/pages/seo-landing/seo-landing-page/seo-landing-page').then(m => m.SeoLandingPageComponent),
    data: {
        seo: {
            titleKey,
            descriptionKey,
            structuredDataBaseKey: baseKey,
            structuredDataFeatureKeys: featureKeys,
            structuredDataFaqKeys: faqKeys,
        },
        seoPage: {
            baseKey,
            featureKeys,
            stepKeys,
            faqKeys,
            relatedPaths,
        },
    },
});

export const routes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./features/public/pages/landing/main').then(m => m.MainComponent),
        canActivate: [mobileShellStartGuard, loggedInGuard],
        canDeactivate: [unsavedChangesGuard],
        data: { seo: { titleKey: 'SEO.LANDING_TITLE', descriptionKey: 'SEO.LANDING_DESCRIPTION' } },
    },
    ...PUBLIC_SEO_LANDING_ROUTES.map(publicSeoLanding),
    ...authRoutes,
    {
        path: 'dashboard',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/dashboard/dashboard.routes'),
        data: { preload: true, seo: { titleKey: 'SEO.DASHBOARD', descriptionKey: 'SEO.DASHBOARD_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'products',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/products/product.routes'),
        data: { preload: true, seo: { titleKey: 'SEO.PRODUCTS', descriptionKey: 'SEO.PRODUCTS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'meals',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/meals/meals.routes'),
        data: { preload: true, seo: { titleKey: 'SEO.MEALS', descriptionKey: 'SEO.MEALS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'recipes',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/recipes/recipe.routes'),
        data: { seo: { titleKey: 'SEO.RECIPES', descriptionKey: 'SEO.RECIPES_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'explore',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/explore/explore.routes'),
        data: { seo: { titleKey: 'SEO.EXPLORE', descriptionKey: 'SEO.EXPLORE_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'shopping-lists',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/shopping-lists/shopping-list.routes'),
        data: { seo: { titleKey: 'SEO.SHOPPING_LISTS', descriptionKey: 'SEO.SHOPPING_LISTS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'goals',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/goals/goals.routes'),
        data: { seo: { titleKey: 'SEO.GOALS', descriptionKey: 'SEO.GOALS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'statistics',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/statistics/statistics.routes'),
        data: { seo: { titleKey: 'SEO.STATISTICS', descriptionKey: 'SEO.STATISTICS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'weight-history',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/weight-history/weight-history.routes'),
        data: { seo: { titleKey: 'SEO.WEIGHT_HISTORY', descriptionKey: 'SEO.WEIGHT_HISTORY_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'waist-history',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/waist-history/waist-history.routes'),
        data: { seo: { titleKey: 'SEO.WAIST_HISTORY', descriptionKey: 'SEO.WAIST_HISTORY_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'cycle-tracking',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/cycle-tracking/cycle-tracking.routes'),
        data: { seo: { titleKey: 'SEO.CYCLE_TRACKING', descriptionKey: 'SEO.CYCLE_TRACKING_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'meal-plans',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/meal-plans/meal-plan.routes'),
        data: { seo: { titleKey: 'SEO.MEAL_PLANS', descriptionKey: 'SEO.MEAL_PLANS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'weekly-check-in',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/weekly-check-in/weekly-check-in.routes'),
        data: { seo: { titleKey: 'SEO.WEEKLY_CHECK_IN', descriptionKey: 'SEO.WEEKLY_CHECK_IN_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'lessons',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/lessons/lesson.routes'),
        data: { seo: { titleKey: 'SEO.LESSONS', descriptionKey: 'SEO.LESSONS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'gamification',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/gamification/gamification.routes'),
        data: { seo: { titleKey: 'SEO.GAMIFICATION', descriptionKey: 'SEO.GAMIFICATION_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'fasting',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/fasting/fasting.routes'),
        data: { seo: { titleKey: 'SEO.FASTING', descriptionKey: 'SEO.FASTING_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'premium',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/premium/premium.routes'),
        data: { seo: { titleKey: 'SEO.PREMIUM', descriptionKey: 'SEO.PREMIUM_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'profile',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/profile/profile.routes'),
        data: { preload: true, seo: { titleKey: 'SEO.PROFILE', descriptionKey: 'SEO.PROFILE_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'dietologist',
        canActivate: [dietologistGuard],
        loadChildren: async () => import('./features/dietologist/dietologist.routes').then(m => m.dietologistRoutes),
        data: { seo: { titleKey: 'SEO.DIETOLOGIST', noIndex: true } },
    },
    {
        path: 'dietologist-invitations/:invitationId',
        canActivate: [authGuard],
        loadComponent: async () =>
            import('./features/dietologist/pages/invitation/dietologist-invitation-page').then(m => m.DietologistInvitationPageComponent),
        data: { seo: { titleKey: 'SEO.DIETOLOGIST', noIndex: true } },
    },
    {
        path: 'recommendations',
        canActivate: [authGuard],
        loadChildren: async () => import('./features/recommendations/recommendations.routes'),
        data: { seo: { titleKey: 'RECOMMENDATIONS.TITLE', noIndex: true } },
    },
    {
        path: 'privacy-policy',
        loadComponent: async () => import('./features/public/pages/privacy-policy/privacy-policy').then(m => m.PrivacyPolicyComponent),
        data: { seo: { titleKey: 'SEO.PRIVACY_POLICY', descriptionKey: 'SEO.PRIVACY_POLICY_DESCRIPTION' } },
    },
    {
        path: '**',
        loadComponent: async () => import('./features/public/pages/not-found/not-found').then(m => m.NotFoundComponent),
        data: { seo: { titleKey: 'SEO.NOT_FOUND', noIndex: true } },
    },
];
