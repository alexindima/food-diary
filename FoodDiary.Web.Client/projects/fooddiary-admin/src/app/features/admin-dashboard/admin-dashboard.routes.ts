import { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminDashboardComponent } from './pages/admin-dashboard.component';

export const adminDashboardRoutes: Routes = [
    {
        path: '',
        component: AdminDashboardComponent,
        canActivate: [adminAuthGuard],
    },
];
