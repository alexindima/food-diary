import { Routes } from '@angular/router';
import { MainComponent } from './components/main/main.component';
import { ProductContainerComponent } from './components/product-container/product-container.component';
import { ProductListPageComponent } from './components/product-container/product-list/product-list-page/product-list-page.component';
import { ProductAddComponent } from './components/product-container/product-manage/product-add/product-add.component';
import { ProductEditComponent } from './components/product-container/product-manage/product-edit/product-edit.component';
import { productResolver } from './resolvers/product.resolver';
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
import { RecipeContainerComponent } from './components/recipe-container/recipe-container.component';
import { RecipeAddComponent } from './components/recipe-container/recipe-manage/recipe-add/recipe-add.component';
import { RecipeListComponent } from './components/recipe-container/recipe-list/recipe-list.component';
import { RecipeEditComponent } from './components/recipe-container/recipe-manage/recipe-edit/recipe-edit.component';
import { recipeResolver } from './resolvers/recipe.resolver';
import { WeightHistoryPageComponent } from './components/weight-history-page/weight-history-page.component';
import { WaistHistoryPageComponent } from './components/waist-history-page/waist-history-page.component';
import { CycleTrackingPageComponent } from './components/cycle-tracking-page/cycle-tracking-page.component';
import { GoalsPageComponent } from './components/goals-page/goals-page.component';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { EmailVerificationPendingComponent } from './components/auth/email-verification-pending/email-verification-pending.component';
import { EmailVerificationComponent } from './components/auth/email-verification/email-verification.component';

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
        path: 'products',
        component: ProductContainerComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: ProductListPageComponent },
            { path: 'add', component: ProductAddComponent },
            {
                path: ':id/edit',
                component: ProductEditComponent,
                resolve: { product: productResolver },
            },
        ],
    },
    {
        path: 'consumptions',
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
    {
        path: 'recipes',
        component: RecipeContainerComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: RecipeListComponent },
            { path: 'add', component: RecipeAddComponent },
            {
                path: ':id/edit',
                component: RecipeEditComponent,
                resolve: { recipe: recipeResolver },
            },
        ],
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
