import { Routes } from '@angular/router';

import { authGuard } from '../../guards/auth.guard';
import { PremiumAccessPageComponent } from './pages/premium-access-page.component';

export const premiumRoutes: Routes = [
    {
        path: 'premium',
        component: PremiumAccessPageComponent,
        canActivate: [authGuard],
    },
];
