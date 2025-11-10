import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ProductListBaseComponent } from '../product-list-base.component';
import {
    TuiButton,
    tuiDialog,
    TuiAlertService,
    TuiIcon,
    TuiLoader,
    TuiTextfieldComponent,
    TuiTextfieldDirective
} from '@taiga-ui/core';
import { Product } from '../../../../types/product.data';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TuiPagination } from '@taiga-ui/kit';
import { TuiSearchComponent } from '@taiga-ui/layout';
import { TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { ProductDetailComponent } from '../../product-detail/product-detail.component';
import { CardComponent } from '../../../shared/card/card.component';
import { BadgeComponent } from '../../../shared/badge/badge.component';
import { finalize } from 'rxjs';

@Component({
    selector: 'fd-product-list-page',
    templateUrl: '../product-list-base.component.html',
    styleUrls: ['./product-list-page.component.less', '../product-list-base.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiButton,
        TuiLoader,
        TuiPagination,
        TuiSearchComponent,
        TuiTextfieldComponent,
        TuiTextfieldControllerModule,
        TuiTextfieldDirective,
        TuiIcon,
        CardComponent,
        BadgeComponent,
    ]
})
export class ProductListPageComponent extends ProductListBaseComponent implements OnInit {
    private readonly dialog = tuiDialog(ProductDetailComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });
    private readonly alertService = inject(TuiAlertService);
    private readonly translateService = inject(TranslateService);
    private isDeleteInProgress = false;

    protected override async onProductClick(product: Product): Promise<void> {
        this.dialog(product).subscribe({
            next: data => {
                if (data.action === 'Edit') {
                    this.navigationService.navigateToProductEdit(data.id);
                } else if (data.action === 'Delete') {
                    if (!product.isOwnedByCurrentUser || this.isDeleteInProgress) {
                        return;
                    }
                    this.isDeleteInProgress = true;
                    this.productData.setLoading(true);
                    this.productService.deleteById(data.id)
                        .pipe(
                            finalize(() => {
                                this.isDeleteInProgress = false;
                            }),
                        )
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
                                this.alertService
                                    .open(
                                        this.translateService.instant('PRODUCT_LIST.DELETE_ERROR'),
                                        { appearance: 'negative' },
                                    )
                                    .subscribe();
                            },
                        });
                }
            },
        });
    }
}
