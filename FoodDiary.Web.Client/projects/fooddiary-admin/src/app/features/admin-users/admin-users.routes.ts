import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminUsersComponent } from './pages/admin-users.component';

export const adminUsersRoutes: Routes = [
    {
        path: '',
        component: AdminUsersComponent,
        canActivate: [adminAuthGuard],
    },
];
