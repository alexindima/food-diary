import type { Routes } from '@angular/router';

import { UnauthorizedComponent } from './pages/unauthorized';

export const adminPublicRoutes: Routes = [
    {
        path: 'unauthorized',
        component: UnauthorizedComponent,
    },
];
