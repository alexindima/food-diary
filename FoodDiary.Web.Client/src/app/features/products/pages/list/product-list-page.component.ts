import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { finalize } from 'rxjs';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { ProductDetailComponent, ProductDetailActionResult } from '../../components/detail/product-detail.component';
import { ProductListBaseComponent } from '../../components/list/product-list-base.component';
import { FavoritesSectionComponent } from '../../../../components/shared/favorites-section/favorites-section.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { ProductCardComponent } from '../../../../components/shared/product-card/product-card.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
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
        this.fdDialogService
            .open<ProductDetailComponent, Product, ProductDetailActionResult>(ProductDetailComponent, {
                size: 'lg',
                data: product,
                panelClass: 'fd-ui-dialog-panel--detail',
                backdropClass: 'fd-ui-dialog-backdrop--detail',
            })
            .afterClosed()
            .subscribe(data => {
                this.loadFavorites();
                this.reloadCurrentPage();

                if (!data) {
                    return;
                }

                if (data.action === 'Edit' || data.action === 'Duplicate') {
                    void this.navigationService.navigateToProductEdit(data.id);
                    return;
                }

                if (data.action === 'Delete') {
                    if (!product.isOwnedByCurrentUser || this.isDeleteInProgress) {
                        return;
                    }
                    this.isDeleteInProgress = true;
                    this.productData.setLoading(true);
                    this.productService
                        .deleteById(data.id)
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
