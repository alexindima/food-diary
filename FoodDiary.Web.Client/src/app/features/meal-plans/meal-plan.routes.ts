import type { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/list/meal-plans-list-page').then(m => m.MealPlansListPageComponent),
    },
    {
        path: ':id',
        loadComponent: async () => import('./pages/detail/meal-plan-detail-page').then(m => m.MealPlanDetailPageComponent),
    },
];

export default routes;
