import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ProductListBaseComponent } from '../product-list-base.component';
import { Product } from '../../../../types/product.data';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    ProductDetailComponent,
    ProductDetailActionResult,
} from '../../product-detail/product-detail.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { finalize } from 'rxjs';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { PageHeaderComponent } from '../../../shared/page-header/page-header.component';
import { PageBodyComponent } from '../../../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { ProductCardComponent } from '../../../shared/product-card/product-card.component';

@Component({
    selector: 'fd-product-list-page',
    templateUrl: '../product-list-base.component.html',
    styleUrls: ['./product-list-page.component.scss', '../product-list-base.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        FdUiIconModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ProductCardComponent,
    ]
})
export class ProductListPageComponent extends ProductListBaseComponent implements OnInit {
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private isDeleteInProgress = false;

    protected override async onProductClick(product: Product): Promise<void> {
        this.fdDialogService
            .open<ProductDetailComponent, Product, ProductDetailActionResult>(ProductDetailComponent, {
                size: 'lg',
                data: product,
            })
            .afterClosed()
            .subscribe(data => {
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
                            error: error => {
                                console.error('Delete product error', error);
                                this.productData.setLoading(false);
                                this.toastService.open(
                                    this.translateService.instant('PRODUCT_LIST.DELETE_ERROR'),
                                    { appearance: 'negative' },
                                );
                            },
                        });
                }
            });
    }
}

