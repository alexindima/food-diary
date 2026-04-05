import { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./pages/list/meal-plans-list-page.component').then(m => m.MealPlansListPageComponent),
    },
    {
        path: ':id',
        loadComponent: () => import('./pages/detail/meal-plan-detail-page.component').then(m => m.MealPlanDetailPageComponent),
    },
];

export default routes;
