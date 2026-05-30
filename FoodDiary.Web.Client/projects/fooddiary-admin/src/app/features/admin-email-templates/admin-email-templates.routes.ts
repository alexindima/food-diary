import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminEmailTemplatesComponent } from './pages/admin-email-templates';

export const adminEmailTemplatesRoutes: Routes = [
    {
        path: '',
        component: AdminEmailTemplatesComponent,
        canActivate: [adminAuthGuard],
    },
];
