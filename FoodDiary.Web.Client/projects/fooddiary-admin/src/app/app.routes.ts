import { Routes } from '@angular/router';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { AdminUsersComponent } from './pages/admin-users/admin-users.component';
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized.component';
import { adminAuthGuard } from './guards/admin-auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: AdminDashboardComponent,
    canActivate: [adminAuthGuard],
  },
  {
    path: 'users',
    component: AdminUsersComponent,
    canActivate: [adminAuthGuard],
  },
  {
    path: 'unauthorized',
    component: UnauthorizedComponent,
  },
  {
    path: '**',
    redirectTo: '',
  },
];
