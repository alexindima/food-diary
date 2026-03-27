import { Routes } from '@angular/router';

import { authGuard } from '../../guards/auth.guard';
import { CycleTrackingPageComponent } from './pages/cycle-tracking-page.component';

export const cycleTrackingRoutes: Routes = [
    {
        path: 'cycle-tracking',
        component: CycleTrackingPageComponent,
        canActivate: [authGuard],
    },
];
