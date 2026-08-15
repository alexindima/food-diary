import type { Routes } from '@angular/router';

import { unsavedChangesGuard } from '../../guards/unsaved-changes.guard';
import { UserManageComponent } from './pages/user-manage/user-manage';

const routes: Routes = [
    {
        path: '',
        component: UserManageComponent,
        canDeactivate: [unsavedChangesGuard],
    },
];

export default routes;
