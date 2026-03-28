import { Routes } from '@angular/router';
import { RecipeContainerComponent } from './pages/container/recipe-container.component';
import { RecipeListComponent } from './pages/list/recipe-list.component';
import { RecipeAddComponent } from './pages/manage/recipe-add.component';
import { RecipeEditComponent } from './pages/manage/recipe-edit.component';
import { recipeResolver } from './resolvers/recipe.resolver';

const routes: Routes = [
    {
        path: '',
        component: RecipeContainerComponent,
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

export default routes;
