import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { EMPTY, finalize, switchMap } from 'rxjs';

import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { ProductDetailActionResult } from '../../components/detail/product-detail-lib/product-detail.types';
import { ProductListBaseComponent } from '../../components/list/product-list-base/product-list-base.component';
import { ProductListEmptyStateComponent } from '../../components/list/product-list-sections/product-list-empty-state/product-list-empty-state.component';
import { ProductListFavoritesComponent } from '../../components/list/product-list-sections/product-list-favorites/product-list-favorites.component';
import { ProductListGroupsComponent } from '../../components/list/product-list-sections/product-list-groups/product-list-groups.component';
import { ProductListOffSectionComponent } from '../../components/list/product-list-sections/product-list-off-section/product-list-off-section.component';
import { ProductListPaginationComponent } from '../../components/list/product-list-sections/product-list-pagination/product-list-pagination.component';
import type { Product } from '../../models/product.data';

@Component({
    selector: 'fd-product-list-page',
    templateUrl: '../../components/list/product-list-base/product-list-base.component.html',
    styleUrls: ['./product-list-page.component.scss', '../../components/list/product-list-base/product-list-base.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ProductListFavoritesComponent,
        ProductListGroupsComponent,
        ProductListEmptyStateComponent,
        ProductListOffSectionComponent,
        ProductListPaginationComponent,
    ],
})
export class ProductListPageComponent extends ProductListBaseComponent {
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private isDeleteInProgress = false;

    protected override async handleProductClickAsync(product: Product): Promise<void> {
        const { ProductDetailComponent } = await import('../../components/detail/product-detail/product-detail.component');
        this.fdDialogService
            .open(ProductDetailComponent, {
                preset: 'detail',
                data: product,
            })
            .afterClosed()
            .pipe(
                switchMap(data => {
                    if (!(data instanceof ProductDetailActionResult)) {
                        return EMPTY;
                    }

                    const result = data;
                    if (result.action === 'FavoriteChanged') {
                        this.loadFavorites();
                        this.reloadCurrentPage();
                        return EMPTY;
                    }

                    if (result.action === 'Edit' || result.action === 'Duplicate') {
                        void this.navigationService.navigateToProductEditAsync(result.id);
                        return EMPTY;
                    }

                    if (!product.isOwnedByCurrentUser || this.isDeleteInProgress) {
                        return EMPTY;
                    }

                    this.isDeleteInProgress = true;
                    this.productData.setLoading(true);
                    return this.productService.deleteById(result.id).pipe(
                        switchMap(() => {
                            this.scrollToTop();
                            return this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchForm.controls.search.value);
                        }),
                        finalize(() => {
                            this.isDeleteInProgress = false;
                        }),
                    );
                }),
            )
            .subscribe({
                error: () => {
                    this.productData.setLoading(false);
                    this.toastService.error(this.translateService.instant('PRODUCT_LIST.DELETE_ERROR'));
                },
            });
    }
}
