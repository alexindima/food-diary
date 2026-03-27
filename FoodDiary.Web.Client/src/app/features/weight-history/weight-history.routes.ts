import { Routes } from '@angular/router';

import { authGuard } from '../../guards/auth.guard';
import { WeightHistoryPageComponent } from './pages/weight-history-page.component';

export const weightHistoryRoutes: Routes = [
    {
        path: 'weight-history',
        component: WeightHistoryPageComponent,
        canActivate: [authGuard],
    },
];
