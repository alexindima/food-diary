import { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminMailInboxComponent } from './pages/admin-mail-inbox.component';

export const adminMailInboxRoutes: Routes = [
    {
        path: '',
        component: AdminMailInboxComponent,
        canActivate: [adminAuthGuard],
    },
];
