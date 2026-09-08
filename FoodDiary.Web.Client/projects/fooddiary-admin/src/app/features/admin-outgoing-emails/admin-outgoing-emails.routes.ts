import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';

export const adminOutgoingEmailsRoutes: Routes = [
    {
        path: '',
        loadComponent: async () => import('./pages/admin-outgoing-emails').then(m => m.AdminOutgoingEmailsComponent),
        canActivate: [adminAuthGuard],
    },
];
