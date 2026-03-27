import { Routes } from '@angular/router';

import { authGuard } from '../../guards/auth.guard';
import { StatisticsComponent } from './pages/statistics.component';

export const statisticsRoutes: Routes = [
    {
        path: 'statistics',
        component: StatisticsComponent,
        canActivate: [authGuard],
    },
];
