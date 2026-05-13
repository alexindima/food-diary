import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { ProductManageFormComponent } from '../../components/manage/product-manage-form/product-manage-form.component';
import type { Product } from '../../models/product.data';

@Component({
    selector: 'fd-product-add-dialog',
    templateUrl: './product-add-dialog.component.html',
    styleUrls: ['./product-add-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ProductManageFormComponent],
})
export class ProductAddDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ProductAddDialogComponent, Product | null>);
    private readonly initialProduct = inject<Product | null>(FD_UI_DIALOG_DATA, { optional: true });

    public onSaved(product: Product): void {
        this.dialogRef.close(product);
    }

    public onCancel(): void {
        this.dialogRef.close(this.initialProduct ?? null);
    }
}
