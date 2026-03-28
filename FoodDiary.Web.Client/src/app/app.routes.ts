import { Routes } from '@angular/router';
import { MainComponent } from './features/public/pages/landing/main.component';
import { NotFoundComponent } from './features/public/pages/not-found/not-found.component';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { authGuard } from './guards/auth.guard';
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
        data: { seo: { titleKey: 'SEO.PRODUCTS', noIndex: true } },
    },
    {
        path: 'meals',
        canActivate: [authGuard],
        loadChildren: () => import('./features/meals/meals.routes'),
        data: { seo: { titleKey: 'SEO.MEALS', noIndex: true } },
    },
    {
        path: 'recipes',
        canActivate: [authGuard],
        loadChildren: () => import('./features/recipes/recipe.routes'),
        data: { seo: { titleKey: 'SEO.RECIPES', noIndex: true } },
    },
    {
        path: 'shopping-lists',
        canActivate: [authGuard],
        loadChildren: () => import('./features/shopping-lists/shopping-list.routes'),
        data: { seo: { titleKey: 'SEO.SHOPPING_LISTS', noIndex: true } },
    },
    {
        path: 'goals',
        canActivate: [authGuard],
        loadChildren: () => import('./features/goals/goals.routes'),
        data: { seo: { titleKey: 'SEO.GOALS', noIndex: true } },
    },
    {
        path: 'statistics',
        canActivate: [authGuard],
        loadChildren: () => import('./features/statistics/statistics.routes'),
        data: { seo: { titleKey: 'SEO.STATISTICS', noIndex: true } },
    },
    {
        path: 'weight-history',
        canActivate: [authGuard],
        loadChildren: () => import('./features/weight-history/weight-history.routes'),
        data: { seo: { titleKey: 'SEO.WEIGHT_HISTORY', noIndex: true } },
    },
    {
        path: 'waist-history',
        canActivate: [authGuard],
        loadChildren: () => import('./features/waist-history/waist-history.routes'),
        data: { seo: { titleKey: 'SEO.WAIST_HISTORY', noIndex: true } },
    },
    {
        path: 'cycle-tracking',
        canActivate: [authGuard],
        loadChildren: () => import('./features/cycle-tracking/cycle-tracking.routes'),
        data: { seo: { titleKey: 'SEO.CYCLE_TRACKING', noIndex: true } },
    },
    {
        path: 'premium',
        canActivate: [authGuard],
        loadChildren: () => import('./features/premium/premium.routes'),
        data: { seo: { titleKey: 'SEO.PREMIUM', noIndex: true } },
    },
    {
        path: 'profile',
        canActivate: [authGuard],
        loadChildren: () => import('./features/profile/profile.routes'),
        data: { seo: { titleKey: 'SEO.PROFILE', noIndex: true } },
    },
    {
        path: '**',
        component: NotFoundComponent,
        data: { seo: { titleKey: 'SEO.NOT_FOUND', noIndex: true } },
    },
];
