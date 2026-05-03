import { type Routes } from '@angular/router';

import { MealContainerComponent } from './pages/container/meal-container.component';
import { mealResolver } from './resolvers/meal.resolver';

const routes: Routes = [
    {
        path: '',
        component: MealContainerComponent,
        children: [
            { path: '', loadComponent: () => import('./pages/list/meal-list.component').then(m => m.MealListComponent) },
            { path: 'add', loadComponent: () => import('./pages/manage/meal-add.component').then(m => m.MealAddComponent) },
            {
                path: ':id/edit',
                loadComponent: () => import('./pages/manage/meal-edit.component').then(m => m.MealEditComponent),
                resolve: { consumption: mealResolver },
            },
        ],
    },
];

export default routes;
