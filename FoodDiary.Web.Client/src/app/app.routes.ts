import { Routes } from '@angular/router';
import { MainComponent } from './components/main/main.component';
import { loggedInGuard } from './guards/logged-in.guard';
import { NotFoundComponent } from './components/not-found/not-found.component';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { EmailVerificationPendingComponent } from './components/auth/email-verification-pending/email-verification-pending.component';
import { EmailVerificationComponent } from './components/auth/email-verification/email-verification.component';
import { PasswordResetComponent } from './components/auth/password-reset/password-reset.component';
import { productRoutes } from './features/products/product.routes';
import { recipeRoutes } from './features/recipes/recipe.routes';
import { shoppingListRoutes } from './features/shopping-lists/shopping-list.routes';
import { goalsRoutes } from './features/goals/goals.routes';
import { statisticsRoutes } from './features/statistics/statistics.routes';
import { weightHistoryRoutes } from './features/weight-history/weight-history.routes';
import { waistHistoryRoutes } from './features/waist-history/waist-history.routes';
import { cycleTrackingRoutes } from './features/cycle-tracking/cycle-tracking.routes';
import { premiumRoutes } from './features/premium/premium.routes';
import { profileRoutes } from './features/profile/profile.routes';
import { mealRoutes } from './features/meals/meals.routes';

export const routes: Routes = [
    { path: '', component: MainComponent, canDeactivate: [unsavedChangesGuard] },
    {
        path: 'auth',
        component: MainComponent,
        data: { openAuth: true },
        canActivate: [loggedInGuard],
    },
    {
        path: 'auth/:mode',
        component: MainComponent,
        data: { openAuth: true },
        canActivate: [loggedInGuard],
    },
    {
        path: 'verify-pending',
        component: EmailVerificationPendingComponent,
    },
    {
        path: 'verify-email',
        component: EmailVerificationComponent,
    },
    {
        path: 'reset-password',
        component: PasswordResetComponent,
    },
    ...productRoutes,
    ...mealRoutes,
    ...recipeRoutes,
    ...shoppingListRoutes,
    ...goalsRoutes,
    ...statisticsRoutes,
    ...weightHistoryRoutes,
    ...waistHistoryRoutes,
    ...cycleTrackingRoutes,
    ...premiumRoutes,
    ...profileRoutes,
    { path: '**', component: NotFoundComponent },
];
