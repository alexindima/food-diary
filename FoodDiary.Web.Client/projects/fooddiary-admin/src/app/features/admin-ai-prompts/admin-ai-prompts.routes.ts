import type { Routes } from '@angular/router';
import type { Observable } from 'rxjs';

import { adminAuthGuard } from '../../guards/admin-auth.guard';
import type { AdminAiPromptsPageComponent } from './pages/admin-ai-prompts';

export const adminAiPromptsRoutes: Routes = [
    {
        path: '',
        canActivate: [adminAuthGuard],
        canDeactivate: [(component: AdminAiPromptsPageComponent): Observable<boolean> => component.canLeave()],
        loadComponent: async () => import('./pages/admin-ai-prompts').then(module => module.AdminAiPromptsPageComponent),
    },
];
