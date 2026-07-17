import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { ProductListBaseComponent } from '../../components/list/product-list-base/product-list-base';
import { ProductListActiveFiltersComponent } from '../../components/list/product-list-sections/product-list-active-filters/product-list-active-filters';
import { ProductListEmptyStateComponent } from '../../components/list/product-list-sections/product-list-empty-state/product-list-empty-state';
import { ProductListFavoritesComponent } from '../../components/list/product-list-sections/product-list-favorites/product-list-favorites';
import { ProductListGroupsComponent } from '../../components/list/product-list-sections/product-list-groups/product-list-groups';
import { ProductListOffSectionComponent } from '../../components/list/product-list-sections/product-list-off-section/product-list-off-section';
import { ProductListPaginationComponent } from '../../components/list/product-list-sections/product-list-pagination/product-list-pagination';
import { ProductListFacade } from '../../lib/list/product-list.facade';
import type { Product } from '../../models/product.data';

@Component({
    selector: 'fd-product-list-page',
    templateUrl: '../../components/list/product-list-base/product-list-base.html',
    styleUrls: ['./product-list-page.scss', '../../components/list/product-list-base/product-list-base.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProductListFacade],
    imports: [
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ProductListActiveFiltersComponent,
        ProductListFavoritesComponent,
        ProductListGroupsComponent,
        ProductListEmptyStateComponent,
        ProductListOffSectionComponent,
        ProductListPaginationComponent,
    ],
})
export class ProductListPageComponent extends ProductListBaseComponent {
    protected override handleProductClick(product: Product): void {
        void this.openProductDetailsAsync(product);
    }

    private async openProductDetailsAsync(product: Product): Promise<void> {
        if (await this.productListFacade.handleProductDetailsAsync(product)) {
            this.scrollToTop();
        }
    }
}
