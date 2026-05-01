import { Routes } from '@angular/router';

import { ProductContainerComponent } from './pages/container/product-container.component';
import { productResolver } from './resolvers/product.resolver';

const routes: Routes = [
    {
        path: '',
        component: ProductContainerComponent,
        children: [
            {
                path: '',
                loadComponent: () => import('./pages/list/product-list-page.component').then(m => m.ProductListPageComponent),
            },
            {
                path: 'add',
                loadComponent: () => import('./pages/manage/product-add.component').then(m => m.ProductAddComponent),
            },
            {
                path: ':id/edit',
                loadComponent: () => import('./pages/manage/product-edit.component').then(m => m.ProductEditComponent),
                resolve: { product: productResolver },
            },
        ],
    },
];

export default routes;
