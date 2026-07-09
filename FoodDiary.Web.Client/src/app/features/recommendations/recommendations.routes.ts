import type { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/recommendations-page/recommendations-page').then(m => m.RecommendationsPageComponent),
    },
];

export default routes;
