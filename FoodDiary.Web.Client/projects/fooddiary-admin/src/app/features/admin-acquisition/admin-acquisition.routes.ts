import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminAcquisitionComponent } from './pages/admin-acquisition';

export const adminAcquisitionRoutes: Routes = [
    {
        path: '',
        component: AdminAcquisitionComponent,
        canActivate: [adminAuthGuard],
    },
];
