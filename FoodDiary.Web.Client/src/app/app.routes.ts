import { Routes } from '@angular/router';
import { MainComponent } from './features/public/pages/landing/main.component';
import { NotFoundComponent } from './features/public/pages/not-found/not-found.component';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { authRoutes } from './features/auth/auth.routes';
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
    ...authRoutes,
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
