import { Routes } from '@angular/router';
import { ProductContainerComponent } from './pages/container/product-container.component';
import { ProductListPageComponent } from './pages/list/product-list-page.component';
import { ProductAddComponent } from './pages/manage/product-add.component';
import { ProductEditComponent } from './pages/manage/product-edit.component';
import { productResolver } from './resolvers/product.resolver';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

const routes: Routes = [
    {
        path: '',
        component: ProductContainerComponent,
        providers: [provideCharts(withDefaultRegisterables())],
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

export default routes;
