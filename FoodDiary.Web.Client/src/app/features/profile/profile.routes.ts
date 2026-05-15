import type { Routes } from '@angular/router';

import { UserManageComponent } from './pages/user-manage/user-manage.component';

const routes: Routes = [
    {
        path: '',
        component: UserManageComponent,
    },
];

export default routes;
