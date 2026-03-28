import { Routes } from '@angular/router';
import { MainComponent } from './features/public/pages/landing/main.component';
import { NotFoundComponent } from './features/public/pages/not-found/not-found.component';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { authGuard } from './guards/auth.guard';
import { authRoutes } from './features/auth/auth.routes';

export const routes: Routes = [
    { path: '', component: MainComponent, canDeactivate: [unsavedChangesGuard] },
    ...authRoutes,
    {
        path: 'products',
        canActivate: [authGuard],
        loadChildren: () => import('./features/products/product.routes'),
    },
    {
        path: 'meals',
        canActivate: [authGuard],
        loadChildren: () => import('./features/meals/meals.routes'),
    },
    {
        path: 'recipes',
        canActivate: [authGuard],
        loadChildren: () => import('./features/recipes/recipe.routes'),
    },
    {
        path: 'shopping-lists',
        canActivate: [authGuard],
        loadChildren: () => import('./features/shopping-lists/shopping-list.routes'),
    },
    {
        path: 'goals',
        canActivate: [authGuard],
        loadChildren: () => import('./features/goals/goals.routes'),
    },
    {
        path: 'statistics',
        canActivate: [authGuard],
        loadChildren: () => import('./features/statistics/statistics.routes'),
    },
    {
        path: 'weight-history',
        canActivate: [authGuard],
        loadChildren: () => import('./features/weight-history/weight-history.routes'),
    },
    {
        path: 'waist-history',
        canActivate: [authGuard],
        loadChildren: () => import('./features/waist-history/waist-history.routes'),
    },
    {
        path: 'cycle-tracking',
        canActivate: [authGuard],
        loadChildren: () => import('./features/cycle-tracking/cycle-tracking.routes'),
    },
    {
        path: 'premium',
        canActivate: [authGuard],
        loadChildren: () => import('./features/premium/premium.routes'),
    },
    {
        path: 'profile',
        canActivate: [authGuard],
        loadChildren: () => import('./features/profile/profile.routes'),
    },
    { path: '**', component: NotFoundComponent },
];
