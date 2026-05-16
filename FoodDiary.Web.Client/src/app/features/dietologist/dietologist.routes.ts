import type { Routes } from '@angular/router';

export const dietologistRoutes: Routes = [
    {
        path: '',
        loadComponent: async () =>
            import('./pages/clients/dietologist-clients-page.component').then(module => module.DietologistClientsPageComponent),
        data: { seo: { titleKey: 'DIETOLOGIST.CLIENTS.TITLE', noIndex: true } },
    },
    {
        path: 'clients/:clientId',
        loadComponent: async () =>
            import('./pages/client-dashboard/client-dashboard.component').then(module => module.ClientDashboardComponent),
        data: { seo: { titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.TITLE', noIndex: true } },
    },
];
