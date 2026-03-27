import { Routes } from '@angular/router';

import { authGuard } from '../../guards/auth.guard';
import { consumptionResolver } from '../../resolvers/consumption.resolver';
import { MealAddComponent } from './pages/manage/meal-add.component';
import { MealEditComponent } from './pages/manage/meal-edit.component';
import { MealContainerComponent } from './pages/container/meal-container.component';
import { MealListComponent } from './pages/list/meal-list.component';

export const mealRoutes: Routes = [
    {
        path: 'meals',
        component: MealContainerComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: MealListComponent },
            { path: 'add', component: MealAddComponent },
            {
                path: ':id/edit',
                component: MealEditComponent,
                resolve: { consumption: consumptionResolver },
            },
        ],
    },
];
