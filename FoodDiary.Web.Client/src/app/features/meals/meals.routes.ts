import { Routes } from '@angular/router';
import { MealContainerComponent } from './pages/container/meal-container.component';
import { MealListComponent } from './pages/list/meal-list.component';
import { MealAddComponent } from './pages/manage/meal-add.component';
import { MealEditComponent } from './pages/manage/meal-edit.component';
import { mealResolver } from './resolvers/meal.resolver';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

const routes: Routes = [
    {
        path: '',
        component: MealContainerComponent,
        providers: [provideCharts(withDefaultRegisterables())],
        children: [
            { path: '', component: MealListComponent },
            { path: 'add', component: MealAddComponent },
            {
                path: ':id/edit',
                component: MealEditComponent,
                resolve: { consumption: mealResolver },
            },
        ],
    },
];

export default routes;
