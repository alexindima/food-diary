import type { Routes } from '@angular/router';

import { MealContainerComponent } from './pages/container/meal-container.component';
import { mealResolver } from './resolvers/meal.resolver';

const routes: Routes = [
    {
        path: '',
        component: MealContainerComponent,
        children: [
            { path: '', loadComponent: async () => import('./pages/list/meal-list.component').then(m => m.MealListComponent) },
            { path: 'add', loadComponent: async () => import('./pages/manage/meal-add/meal-add.component').then(m => m.MealAddComponent) },
            {
                path: ':id/edit',
                loadComponent: async () => import('./pages/manage/meal-edit/meal-edit.component').then(m => m.MealEditComponent),
                resolve: { consumption: mealResolver },
            },
        ],
    },
];

export default routes;
