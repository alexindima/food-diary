import type { Routes } from '@angular/router';

import { unsavedChangesGuard } from '../../guards/unsaved-changes.guard';

const routes: Routes = [
    {
        path: '',
        canDeactivate: [unsavedChangesGuard],
        loadComponent: async () => import('./pages/dashboard').then(m => m.DashboardComponent),
    },
];

export default routes;
