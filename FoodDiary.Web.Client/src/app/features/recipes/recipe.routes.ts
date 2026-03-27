import { Route } from '@angular/router';
import { authGuard } from '../../guards/auth.guard';
import { RecipeContainerComponent } from './pages/container/recipe-container.component';
import { RecipeListComponent } from './pages/list/recipe-list.component';
import { RecipeAddComponent } from './pages/manage/recipe-add.component';
import { RecipeEditComponent } from './pages/manage/recipe-edit.component';
import { recipeResolver } from './resolvers/recipe.resolver';

export const recipeRoutes: Route[] = [
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
];
