import { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminEmailTemplatesComponent } from './pages/admin-email-templates.component';

export const adminEmailTemplatesRoutes: Routes = [
    {
        path: '',
        component: AdminEmailTemplatesComponent,
        canActivate: [adminAuthGuard],
    },
];
