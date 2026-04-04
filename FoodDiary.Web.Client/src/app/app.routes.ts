import { Routes } from '@angular/router';
import { MainComponent } from './features/public/pages/landing/main.component';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { authGuard } from './guards/auth.guard';
import { dietologistGuard } from './guards/dietologist.guard';
import { authRoutes } from './features/auth/auth.routes';

export const routes: Routes = [
    {
        path: '',
        component: MainComponent,
        canDeactivate: [unsavedChangesGuard],
        data: { seo: { titleKey: null, descriptionKey: 'SEO.LANDING_DESCRIPTION' } },
    },
    ...authRoutes,
    {
        path: 'products',
        canActivate: [authGuard],
        loadChildren: () => import('./features/products/product.routes'),
        data: { seo: { titleKey: 'SEO.PRODUCTS', descriptionKey: 'SEO.PRODUCTS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'meals',
        canActivate: [authGuard],
        loadChildren: () => import('./features/meals/meals.routes'),
        data: { seo: { titleKey: 'SEO.MEALS', descriptionKey: 'SEO.MEALS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'recipes',
        canActivate: [authGuard],
        loadChildren: () => import('./features/recipes/recipe.routes'),
        data: { seo: { titleKey: 'SEO.RECIPES', descriptionKey: 'SEO.RECIPES_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'shopping-lists',
        canActivate: [authGuard],
        loadChildren: () => import('./features/shopping-lists/shopping-list.routes'),
        data: { seo: { titleKey: 'SEO.SHOPPING_LISTS', descriptionKey: 'SEO.SHOPPING_LISTS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'goals',
        canActivate: [authGuard],
        loadChildren: () => import('./features/goals/goals.routes'),
        data: { seo: { titleKey: 'SEO.GOALS', descriptionKey: 'SEO.GOALS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'statistics',
        canActivate: [authGuard],
        loadChildren: () => import('./features/statistics/statistics.routes'),
        data: { seo: { titleKey: 'SEO.STATISTICS', descriptionKey: 'SEO.STATISTICS_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'weight-history',
        canActivate: [authGuard],
        loadChildren: () => import('./features/weight-history/weight-history.routes'),
        data: { seo: { titleKey: 'SEO.WEIGHT_HISTORY', descriptionKey: 'SEO.WEIGHT_HISTORY_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'waist-history',
        canActivate: [authGuard],
        loadChildren: () => import('./features/waist-history/waist-history.routes'),
        data: { seo: { titleKey: 'SEO.WAIST_HISTORY', descriptionKey: 'SEO.WAIST_HISTORY_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'cycle-tracking',
        canActivate: [authGuard],
        loadChildren: () => import('./features/cycle-tracking/cycle-tracking.routes'),
        data: { seo: { titleKey: 'SEO.CYCLE_TRACKING', descriptionKey: 'SEO.CYCLE_TRACKING_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'fasting',
        canActivate: [authGuard],
        loadChildren: () => import('./features/fasting/fasting.routes'),
        data: { seo: { titleKey: 'SEO.FASTING', descriptionKey: 'SEO.FASTING_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'premium',
        canActivate: [authGuard],
        loadChildren: () => import('./features/premium/premium.routes'),
        data: { seo: { titleKey: 'SEO.PREMIUM', descriptionKey: 'SEO.PREMIUM_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'profile',
        canActivate: [authGuard],
        loadChildren: () => import('./features/profile/profile.routes'),
        data: { seo: { titleKey: 'SEO.PROFILE', descriptionKey: 'SEO.PROFILE_DESCRIPTION', noIndex: true } },
    },
    {
        path: 'dietologist',
        canActivate: [dietologistGuard],
        loadChildren: () => import('./features/dietologist/dietologist.routes').then(m => m.dietologistRoutes),
        data: { seo: { titleKey: 'SEO.DIETOLOGIST', noIndex: true } },
    },
    {
        path: '**',
        loadComponent: () => import('./features/public/pages/not-found/not-found.component').then(m => m.NotFoundComponent),
        data: { seo: { titleKey: 'SEO.NOT_FOUND', noIndex: true } },
    },
];
