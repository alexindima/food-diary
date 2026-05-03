import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize } from 'rxjs';

import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { FavoritesSectionComponent } from '../../../../components/shared/favorites-section/favorites-section.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { ProductCardComponent } from '../../../../components/shared/product-card/product-card.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import type { ProductDetailActionResult } from '../../components/detail/product-detail.component';
import { ProductListBaseComponent } from '../../components/list/product-list-base.component';
import { Product } from '../../models/product.data';

@Component({
    selector: 'fd-product-list-page',
    templateUrl: '../../components/list/product-list-base.component.html',
    styleUrls: ['./product-list-page.component.scss', '../../components/list/product-list-base.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        DecimalPipe,
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiPaginationComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        FavoritesSectionComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ProductCardComponent,
    ],
})
export class ProductListPageComponent extends ProductListBaseComponent {
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private isDeleteInProgress = false;

    protected override async onProductClick(product: Product): Promise<void> {
        const { ProductDetailComponent } = await import('../../components/detail/product-detail.component');
        this.fdDialogService
            .open(ProductDetailComponent, {
                preset: 'detail',
                data: product,
            })
            .afterClosed()
            .subscribe(data => {
                const result = data as ProductDetailActionResult | undefined;

                if (!result) {
                    return;
                }

                if (result.action === 'FavoriteChanged') {
                    this.loadFavorites();
                    this.reloadCurrentPage();
                    return;
                }

                if (result.action === 'Edit' || result.action === 'Duplicate') {
                    void this.navigationService.navigateToProductEdit(result.id);
                    return;
                }

                if (result.action === 'Delete') {
                    if (!product.isOwnedByCurrentUser || this.isDeleteInProgress) {
                        return;
                    }
                    this.isDeleteInProgress = true;
                    this.productData.setLoading(true);
                    this.productService
                        .deleteById(result.id)
                        .pipe(finalize(() => (this.isDeleteInProgress = false)))
                        .subscribe({
                            next: () => {
                                this.scrollToTop();
                                this.loadProducts(
                                    this.currentPageIndex + 1,
                                    this.pageSize,
                                    this.searchForm.controls.search.value,
                                ).subscribe();
                            },
                            error: () => {
                                this.productData.setLoading(false);
                                this.toastService.error(this.translateService.instant('PRODUCT_LIST.DELETE_ERROR'));
                            },
                        });
                }
            });
    }
}
