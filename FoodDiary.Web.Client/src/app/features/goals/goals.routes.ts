import type { Routes } from '@angular/router';

import { unsavedChangesGuard } from '../../guards/unsaved-changes.guard';
import { GoalsPageComponent } from './pages/goals-page';

const routes: Routes = [
    {
        path: '',
        component: GoalsPageComponent,
        canDeactivate: [unsavedChangesGuard],
    },
];

export default routes;
