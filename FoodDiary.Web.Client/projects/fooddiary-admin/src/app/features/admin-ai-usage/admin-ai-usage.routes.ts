import { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminAiUsageComponent } from './pages/admin-ai-usage.component';

export const adminAiUsageRoutes: Routes = [
    {
        path: '',
        component: AdminAiUsageComponent,
        canActivate: [adminAuthGuard],
    },
];
