import { Routes } from '@angular/router';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { AdminUsersComponent } from './pages/admin-users/admin-users.component';
import { UnauthorizedComponent } from './pages/unauthorized/unauthorized.component';
import { adminAuthGuard } from './guards/admin-auth.guard';
import { AdminAiUsageComponent } from './pages/admin-ai-usage/admin-ai-usage.component';

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
    path: 'ai-usage',
    component: AdminAiUsageComponent,
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
