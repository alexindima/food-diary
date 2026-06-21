import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { take } from 'rxjs';

import { ProductListBaseComponent } from '../../components/list/product-list-base/product-list-base';
import { ProductListFacade } from '../../lib/list/product-list.facade';
import type { Product } from '../../models/product.data';
import { ProductAddDialogComponent } from '../product-add-dialog/product-add-dialog';
import type { ProductSelectItemViewModel } from './product-list-dialog.types';
import { ProductListDialogContentComponent } from './product-list-dialog-content';

@Component({
    selector: 'fd-product-list-dialog',
    templateUrl: './product-list-dialog.html',
    styleUrls: ['./product-list-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProductListFacade],
    imports: [
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        ProductListDialogContentComponent,
    ],
})
export class ProductListDialogComponent extends ProductListBaseComponent {
    public readonly embedded = input<boolean>(false);
    public readonly productSelected = output<Product>();
    protected readonly filterIcon = computed(() => (this.onlyMineFilter() ? 'person' : 'groups'));
    protected readonly productItems = computed<ProductSelectItemViewModel[]>(() =>
        this.productData.items().map(product => ({
            product,
            imageUrl: this.resolveImage(product),
        })),
    );

    private readonly dialogRef = inject(FdUiDialogRef<ProductListDialogComponent, Product | null>, {
        optional: true,
    });

    protected override onAddProductClick(): void {
        this.fdDialogService
            .open<ProductAddDialogComponent, Product | null, Product | null>(ProductAddDialogComponent, {
                preset: 'fullscreen',
            })
            .afterClosed()
            .pipe(take(1))
            .subscribe(product => {
                if (product !== null && product !== undefined) {
                    this.handleSelection(product);
                }
            });
    }

    protected override async handleProductClickAsync(product: Product): Promise<void> {
        this.handleSelection(product);
        await Promise.resolve(undefined);
    }

    private handleSelection(product: Product): void {
        if (!this.embedded() && this.dialogRef !== null) {
            this.dialogRef.close(product);
        } else {
            this.productSelected.emit(product);
        }
    }
}
