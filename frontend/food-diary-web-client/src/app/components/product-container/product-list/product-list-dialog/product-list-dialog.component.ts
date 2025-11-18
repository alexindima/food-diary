import { ChangeDetectionStrategy, Component, EventEmitter, Input, Optional, Output, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ProductListBaseComponent } from '../product-list-base.component';
import { TuiIcon, TuiLoader } from '@taiga-ui/core';
import { Product } from '../../../../types/product.data';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiPagination } from '@taiga-ui/kit';
import { ProductAddDialogComponent } from '../../product-manage/product-add-dialog/product-add-dialog.component';
import { BadgeComponent } from '../../../shared/badge/badge.component';
import { FdUiEntityCardComponent } from '../../../../ui-kit/entity-card/fd-ui-entity-card.component';
import { FdUiInputComponent } from '../../../../ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from '../../../../ui-kit/button/fd-ui-button.component';
import { FdUiCheckboxComponent } from '../../../../ui-kit/checkbox/fd-ui-checkbox.component';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
    selector: 'fd-product-list-dialog',
    standalone: true,
    templateUrl: '../product-list-base.component.html',
    styleUrls: ['./product-list-dialog.component.less', '../product-list-base.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiLoader,
        TuiPagination,
        TuiIcon,
        BadgeComponent,
        FdUiEntityCardComponent,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiCheckboxComponent,
    ]
})
export class ProductListDialogComponent extends ProductListBaseComponent {
    @Input() public embedded: boolean = false;
    @Output() public productSelected = new EventEmitter<Product>();

    private readonly dialogRef = inject(MatDialogRef<ProductListDialogComponent, Product | null>, {
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
        if (!this.embedded && this.dialogRef) {
            this.dialogRef.close(product);
        } else {
            this.productSelected.emit(product);
        }
    }
}
