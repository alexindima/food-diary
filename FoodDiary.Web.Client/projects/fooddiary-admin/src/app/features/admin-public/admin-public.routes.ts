import { Routes } from '@angular/router';

import { UnauthorizedComponent } from './pages/unauthorized.component';

export const adminPublicRoutes: Routes = [
    {
        path: 'unauthorized',
        component: UnauthorizedComponent,
    },
];
