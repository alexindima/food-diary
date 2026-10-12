import type { Routes } from '@angular/router';

import { editorUnsavedChangesGuard } from '../../shared/forms/editor-unsaved-changes.guard';
import { MealContainerComponent } from './pages/container/meal-container';
import { mealResolver } from './resolvers/meal.resolver';

const routes: Routes = [
    {
        path: '',
        component: MealContainerComponent,
        children: [
            { path: '', loadComponent: async () => import('./pages/list/meal-list').then(m => m.MealListComponent) },
            {
                path: 'add',
                canDeactivate: [editorUnsavedChangesGuard],
                loadComponent: async () => import('./pages/manage/meal-add/meal-add').then(m => m.MealAddComponent),
            },
            {
                path: ':id/edit',
                canDeactivate: [editorUnsavedChangesGuard],
                loadComponent: async () => import('./pages/manage/meal-edit/meal-edit').then(m => m.MealEditComponent),
                resolve: { meal: mealResolver },
            },
        ],
    },
];

export default routes;
