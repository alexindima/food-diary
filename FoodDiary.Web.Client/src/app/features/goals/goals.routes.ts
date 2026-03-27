import { Routes } from '@angular/router';
import { authGuard } from '../../guards/auth.guard';
import { GoalsPageComponent } from './pages/goals-page.component';

export const goalsRoutes: Routes = [
    {
        path: 'goals',
        component: GoalsPageComponent,
        canActivate: [authGuard],
    },
];
