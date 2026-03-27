import { Routes } from '@angular/router';

import { authGuard } from '../../guards/auth.guard';
import { WaistHistoryPageComponent } from './pages/waist-history-page.component';

export const waistHistoryRoutes: Routes = [
    {
        path: 'waist-history',
        component: WaistHistoryPageComponent,
        canActivate: [authGuard],
    },
];
