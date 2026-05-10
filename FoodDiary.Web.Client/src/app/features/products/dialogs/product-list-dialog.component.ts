import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';

import { ProductListBaseComponent } from '../components/list/product-list-base.component';
import { type Product } from '../models/product.data';
import { ProductAddDialogComponent } from './product-add-dialog.component';

interface ProductSelectItemViewModel {
    product: Product;
    imageUrl: string | undefined;
}

@Component({
    selector: 'fd-product-list-dialog',
    standalone: true,
    templateUrl: './product-list-dialog.component.html',
    styleUrls: ['./product-list-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiLoaderComponent,
        FdUiPaginationComponent,
        FdUiIconComponent,
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
    public override onAddProductClick(): void {
        this.fdDialogService
            .open<ProductAddDialogComponent, Product | null, Product | null>(ProductAddDialogComponent, {
                preset: 'fullscreen',
            })
            .afterClosed()
            .subscribe(product => {
                if (product) {
                    this.handleSelection(product);
                }
            });
    }

    protected override handleProductClickAsync(product: Product): Promise<void> {
        this.handleSelection(product);

        return Promise.resolve();
    }

    private handleSelection(product: Product): void {
        if (!this.embedded() && this.dialogRef) {
            this.dialogRef.close(product);
        } else {
            this.productSelected.emit(product);
        }
    }
}
