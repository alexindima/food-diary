import { Routes } from '@angular/router';
import { AuthComponent } from './components/auth/auth.component';
import { MainComponent } from './components/main/main.component';
import { FoodContainerComponent } from './components/food-container/food-container.component';
import { FoodListPageComponent } from './components/food-container/food-list/food-list-page/food-list-page.component';
import { FoodAddComponent } from './components/food-container/food-manage/food-add/food-add.component';
import { FoodEditComponent } from './components/food-container/food-manage/food-edit/food-edit.component';
import { foodResolver } from './resolvers/food.resolver';
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

export const routes: Routes = [
    { path: '', component: MainComponent },
    {
        path: 'auth',
        component: AuthComponent,
        canActivate: [loggedInGuard],
    },
    {
        path: 'auth/:mode',
        component: AuthComponent,
        canActivate: [loggedInGuard],
    },
    {
        path: 'foods',
        component: FoodContainerComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: FoodListPageComponent },
            { path: 'add', component: FoodAddComponent },
            {
                path: ':id/edit',
                component: FoodEditComponent,
                resolve: { food: foodResolver },
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
        path: 'statistics',
        component: StatisticsComponent,
        canActivate: [authGuard],
    },
    {
        path: 'profile',
        component: UserManageComponent,
        canActivate: [authGuard],
    },
    { path: '**', component: NotFoundComponent },
];
