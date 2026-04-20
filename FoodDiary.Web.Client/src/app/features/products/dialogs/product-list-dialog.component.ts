import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { FdUiHintDirective } from 'fd-ui-kit';
import { ProductListBaseComponent } from '../components/list/product-list-base.component';
import { Product } from '../models/product.data';
import { TranslatePipe } from '@ngx-translate/core';
import { ProductAddDialogComponent } from './product-add-dialog.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

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

    private readonly dialogRef = inject(FdUiDialogRef<ProductListDialogComponent, Product | null>, {
        optional: true,
    });
    public override async onAddProductClick(): Promise<void> {
        this.fdDialogService
            .open<ProductAddDialogComponent, Product | null, Product | null>(ProductAddDialogComponent, {
                size: 'lg',
                panelClass: 'fd-ui-dialog-panel--fullscreen',
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
