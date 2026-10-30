import { ChangeDetectionStrategy, Component, inject, Injector } from '@angular/core';
import { FormField } from '@angular/forms/signals';
import { convertToParamMap, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormDirective } from 'fd-ui-kit/form/fd-ui-form.directive';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { ActiveFiltersComponent } from '../../../../components/shared/active-filters/active-filters';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card';
import { devQuery } from '../../../../shared/development/development-input';
import { connectDevelopmentScreen, DevelopmentToolError } from '../../../../shared/development/development-tools';
import type { Product } from '../../../../shared/models/product.data';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { ProductListBaseComponent } from '../../components/list/product-list-base/product-list-base';
import { ProductListEmptyStateComponent } from '../../components/list/product-list-sections/product-list-empty-state/product-list-empty-state';
import { ProductListGroupsComponent } from '../../components/list/product-list-sections/product-list-groups/product-list-groups';
import { ProductListOffSectionComponent } from '../../components/list/product-list-sections/product-list-off-section/product-list-off-section';
import { ProductListPaginationComponent } from '../../components/list/product-list-sections/product-list-pagination/product-list-pagination';
import { ProductListFacade } from '../../lib/list/product-list.facade';
import { readProductListQuery } from '../../lib/list/product-list-query';
import { PRODUCT_LIST_QUERY_STATE } from '../../lib/list/product-list-query-state';
import { ProductListRouteStateFacade } from '../../lib/list/product-list-route-state.facade';

@Component({
    selector: 'fd-product-list-page',
    templateUrl: '../../components/list/product-list-base/product-list-base.html',
    styleUrls: ['./product-list-page.scss', '../../components/list/product-list-base/product-list-base.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        ProductListFacade,
        ProductListRouteStateFacade,
        { provide: PRODUCT_LIST_QUERY_STATE, useExisting: ProductListRouteStateFacade },
    ],
    imports: [
        FormField,
        FdUiFormDirective,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ActiveFiltersComponent,
        ProductListGroupsComponent,
        ProductListEmptyStateComponent,
        ProductListOffSectionComponent,
        ProductListPaginationComponent,
    ],
})
export class ProductListPageComponent extends ProductListBaseComponent {
    public constructor() {
        super();
        const injector = inject(Injector);
        const fields = ['search', 'page', 'onlyMine', 'types', 'caloriesFrom', 'caloriesTo', 'hasImage'];
        connectDevelopmentScreen({
            kind: 'product-list',
            read: () => ({
                filterFields: fields,
                filters: {
                    search: this.searchValue(),
                    onlyMine: this.onlyMineFilter(),
                    types: this.productListFacade.selectedProductTypes(),
                    caloriesFrom: this.caloriesFromFilter(),
                    caloriesTo: this.caloriesToFilter(),
                    hasImage: this.hasImageFilter(),
                },
                page: this.productData.currentPage,
                totalItems: this.productData.totalItems,
                totalPages: this.productData.totalPages,
                provenance: this.productData.provenance(),
                items: this.productData.items().map(product => ({ id: product.id, name: product.name, unit: product.baseUnit })),
                loading: this.productData.isLoading(),
                error: this.errorKey(),
            }),
            setFiltersAsync: async filters => {
                await injector
                    .get(ProductListRouteStateFacade)
                    .writeAsync(readProductListQuery(convertToParamMap(devQuery(filters, fields))));
            },
            openEntityAsync: async id => {
                if (!this.productData.items().some(product => product.id === id)) {
                    throw new DevelopmentToolError('ID is not in the current product list.');
                }
                await injector.get(Router).navigate(['/products', id, 'edit']);
            },
        });
    }

    protected override handleProductClick(product: Product): void {
        void this.openProductDetailsAsync(product);
    }

    private async openProductDetailsAsync(product: Product): Promise<void> {
        if (await this.productListFacade.handleProductDetailsAsync(product)) {
            this.scrollToTop();
        }
    }
}
