import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminModerationComponent } from './pages/admin-moderation.component';

export const adminModerationRoutes: Routes = [{ path: '', component: AdminModerationComponent, canActivate: [adminAuthGuard] }];
