import type { Routes } from '@angular/router';

import { ProductContainerComponent } from './pages/container/product-container';
import { productResolver } from './resolvers/product.resolver';

const routes: Routes = [
    {
        path: '',
        component: ProductContainerComponent,
        children: [
            {
                path: '',
                loadComponent: async () => import('./pages/list/product-list-page').then(m => m.ProductListPageComponent),
            },
            {
                path: 'add',
                loadComponent: async () => import('./pages/manage/product-add/product-add').then(m => m.ProductAddComponent),
            },
            {
                path: ':id/edit',
                loadComponent: async () => import('./pages/manage/product-edit/product-edit').then(m => m.ProductEditComponent),
                resolve: { product: productResolver },
            },
        ],
    },
];

export default routes;
