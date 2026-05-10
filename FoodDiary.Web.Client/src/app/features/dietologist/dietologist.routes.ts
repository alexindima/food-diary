import type { Routes } from '@angular/router';

export const dietologistRoutes: Routes = [
    {
        path: '',
        loadComponent: () => import('./pages/clients/dietologist-clients-page.component').then(m => m.DietologistClientsPageComponent),
        data: { seo: { titleKey: 'DIETOLOGIST.CLIENTS.TITLE', noIndex: true } },
    },
    {
        path: 'clients/:clientId',
        loadComponent: () => import('./pages/client-dashboard/client-dashboard.component').then(m => m.ClientDashboardComponent),
        data: { seo: { titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.TITLE', noIndex: true } },
    },
];
