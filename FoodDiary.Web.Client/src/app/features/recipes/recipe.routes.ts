import type { Routes } from '@angular/router';

import { RecipeContainerComponent } from './pages/container/recipe-container.component';
import { recipeResolver } from './resolvers/recipe.resolver';

const routes: Routes = [
    {
        path: '',
        component: RecipeContainerComponent,
        children: [
            {
                path: '',
                loadComponent: () => import('./pages/list/recipe-list.component').then(m => m.RecipeListComponent),
            },
            {
                path: 'add',
                loadComponent: () => import('./pages/manage/recipe-add.component').then(m => m.RecipeAddComponent),
            },
            {
                path: ':id/edit',
                loadComponent: () => import('./pages/manage/recipe-edit.component').then(m => m.RecipeEditComponent),
                resolve: { recipe: recipeResolver },
            },
        ],
    },
];

export default routes;
