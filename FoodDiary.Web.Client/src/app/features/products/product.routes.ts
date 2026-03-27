import { Route } from '@angular/router';
import { authGuard } from '../../guards/auth.guard';
import { ProductContainerComponent } from './pages/container/product-container.component';
import { ProductListPageComponent } from './pages/list/product-list-page.component';
import { ProductAddComponent } from './pages/manage/product-add.component';
import { ProductEditComponent } from './pages/manage/product-edit.component';
import { productResolver } from './resolvers/product.resolver';

export const productRoutes: Route[] = [
    {
        path: 'products',
        component: ProductContainerComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: ProductListPageComponent },
            { path: 'add', component: ProductAddComponent },
            {
                path: ':id/edit',
                component: ProductEditComponent,
                resolve: { product: productResolver },
            },
        ],
    },
];
