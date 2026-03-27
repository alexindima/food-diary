import { Routes } from '@angular/router';
import { authGuard } from '../../guards/auth.guard';
import { ShoppingListPageComponent } from './pages/shopping-list-page.component';

export const shoppingListRoutes: Routes = [
    {
        path: 'shopping-lists',
        component: ShoppingListPageComponent,
        canActivate: [authGuard],
    },
];
