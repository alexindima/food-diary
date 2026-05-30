import type { Routes } from '@angular/router';

import { ShoppingListPageComponent } from './pages/shopping-list-page/shopping-list-page';

const routes: Routes = [
    {
        path: '',
        component: ShoppingListPageComponent,
    },
];

export default routes;
