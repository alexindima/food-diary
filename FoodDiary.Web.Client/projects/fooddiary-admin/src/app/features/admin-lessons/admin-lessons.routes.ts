import type { Routes } from '@angular/router';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import { AdminLessonsComponent } from './pages/admin-lessons.component';

export const adminLessonsRoutes: Routes = [
    {
        path: '',
        component: AdminLessonsComponent,
        canActivate: [adminAuthGuard],
    },
];
