import type { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/dashboard').then(m => m.DashboardComponent),
    },
];

export default routes;
