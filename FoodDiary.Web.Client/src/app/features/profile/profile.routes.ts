import { Routes } from '@angular/router';

import { authGuard } from '../../guards/auth.guard';
import { UserManageComponent } from './pages/user-manage.component';

export const profileRoutes: Routes = [
    {
        path: 'profile',
        component: UserManageComponent,
        canActivate: [authGuard],
    },
];
