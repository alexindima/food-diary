import { Routes } from '@angular/router';
import { MainComponent } from './components/main/main.component';
import { ConsumptionContainerComponent } from './components/consumption-container/consumption-container.component';
import { ConsumptionListComponent } from './components/consumption-container/consumption-list/consumption-list.component';
import { ConsumptionAddComponent } from './components/consumption-container/consumption-manage/consumption-add/consumption-add.component';
import { ConsumptionEditComponent } from './components/consumption-container/consumption-manage/consumption-edit/consumption-edit.component';
import { consumptionResolver } from './resolvers/consumption.resolver';
import { StatisticsComponent } from './components/statistics/statistics.component';
import { UserManageComponent } from './components/user-manage/user-manage.component';
import { loggedInGuard } from './guards/logged-in.guard';
import { authGuard } from './guards/auth.guard';
import { NotFoundComponent } from './components/not-found/not-found.component';
import { WeightHistoryPageComponent } from './components/weight-history-page/weight-history-page.component';
import { WaistHistoryPageComponent } from './components/waist-history-page/waist-history-page.component';
import { CycleTrackingPageComponent } from './components/cycle-tracking-page/cycle-tracking-page.component';
import { GoalsPageComponent } from './components/goals-page/goals-page.component';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { EmailVerificationPendingComponent } from './components/auth/email-verification-pending/email-verification-pending.component';
import { EmailVerificationComponent } from './components/auth/email-verification/email-verification.component';
import { PasswordResetComponent } from './components/auth/password-reset/password-reset.component';
import { ShoppingListPageComponent } from './components/shopping-list-page/shopping-list-page.component';
import { PremiumAccessPageComponent } from './components/premium-access-page/premium-access-page.component';
import { productRoutes } from './features/products/product.routes';
import { recipeRoutes } from './features/recipes/recipe.routes';

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
    {
        path: 'meals',
        component: ConsumptionContainerComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: ConsumptionListComponent },
            { path: 'add', component: ConsumptionAddComponent },
            {
                path: ':id/edit',
                component: ConsumptionEditComponent,
                resolve: { consumption: consumptionResolver },
            },
        ],
    },
    ...recipeRoutes,
    {
        path: 'shopping-lists',
        component: ShoppingListPageComponent,
        canActivate: [authGuard],
    },
    {
        path: 'statistics',
        component: StatisticsComponent,
        canActivate: [authGuard],
    },
    {
        path: 'profile',
        component: UserManageComponent,
        canActivate: [authGuard],
    },
    {
        path: 'premium',
        component: PremiumAccessPageComponent,
        canActivate: [authGuard],
    },
    {
        path: 'goals',
        component: GoalsPageComponent,
        canActivate: [authGuard],
    },
    {
        path: 'weight-history',
        component: WeightHistoryPageComponent,
        canActivate: [authGuard],
    },
    {
        path: 'waist-history',
        component: WaistHistoryPageComponent,
        canActivate: [authGuard],
    },
    {
        path: 'cycle-tracking',
        component: CycleTrackingPageComponent,
        canActivate: [authGuard],
    },
    { path: '**', component: NotFoundComponent },
];
