import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ProductListBaseComponent } from '../product-list-base.component';
import { Product } from '../../../../types/product.data';
import { TranslatePipe } from '@ngx-translate/core';
import { ProductAddDialogComponent } from '../../product-manage/product-add-dialog/product-add-dialog.component';
import { FdUiPlainInputComponent } from 'fd-ui-kit/plain-input/fd-ui-plain-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { PageHeaderComponent } from '../../../shared/page-header/page-header.component';
import { PageBodyComponent } from '../../../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { ProductCardComponent } from '../../../shared/product-card/product-card.component';

@Component({
    selector: 'fd-product-list-dialog',
    standalone: true,
    templateUrl: '../product-list-base.component.html',
    styleUrls: ['./product-list-dialog.component.scss', '../product-list-base.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiPlainInputComponent,
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
export class ProductListDialogComponent extends ProductListBaseComponent {
    public readonly embedded = input<boolean>(false);
    public readonly productSelected = output<Product>();

    private readonly dialogRef = inject(FdUiDialogRef<ProductListDialogComponent, Product | null>, {
        optional: true,
    });
    public override async onAddProductClick(): Promise<void> {
        this.fdDialogService
            .open<ProductAddDialogComponent, Product | null, Product | null>(ProductAddDialogComponent, {
                size: 'lg',
            })
            .afterClosed()
            .subscribe(product => {
                if (product) {
                    this.handleSelection(product);
                }
            });
    }

    protected override async onProductClick(product: Product): Promise<void> {
        this.handleSelection(product);
    }

    private handleSelection(product: Product): void {
        if (!this.embedded() && this.dialogRef) {
            this.dialogRef.close(product);
        } else {
            this.productSelected.emit(product);
        }
    }
}
