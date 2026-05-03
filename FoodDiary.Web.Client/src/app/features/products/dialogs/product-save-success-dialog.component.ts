import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { type RedirectAction } from '../lib/product-manage.facade';

export interface ProductSaveSuccessDialogData {
    isEdit: boolean;
}

@Component({
    selector: 'fd-product-save-success-dialog',
    standalone: true,
    imports: [FdUiDialogComponent, FdUiButtonComponent, TranslatePipe],
    templateUrl: './product-save-success-dialog.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductSaveSuccessDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ProductSaveSuccessDialogComponent, RedirectAction>);
    private readonly data = inject(FD_UI_DIALOG_DATA) as ProductSaveSuccessDialogData;
    protected readonly titleKey = computed(() => (this.data.isEdit ? 'PRODUCT_DETAIL.EDIT_SUCCESS' : 'PRODUCT_DETAIL.CREATE_SUCCESS'));

    public close(action: RedirectAction): void {
        this.dialogRef.close(action);
    }
}
