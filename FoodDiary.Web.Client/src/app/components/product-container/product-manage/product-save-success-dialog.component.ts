import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { TranslatePipe } from '@ngx-translate/core';
import { RedirectAction } from './base-product-manage.component';

export interface ProductSaveSuccessDialogData {
    isEdit: boolean;
}

@Component({
    selector: 'fd-product-save-success-dialog',
    standalone: true,
    imports: [FdUiDialogComponent, FdUiButtonComponent, TranslatePipe],
    template: `
        <fd-ui-dialog
            [title]="titleKey | translate"
            size="sm"
            [dismissible]="false"
        >
            <div class="product-manage__dialog">
                <p class="product-manage__dialog-title">{{ titleKey | translate }}</p>
                <div class="product-manage__dialog-buttons">
                    <fd-ui-button fill="text" size="sm" (click)="close('Home')">
                        {{ 'PRODUCT_DETAIL.GO_TO_HOME_BUTTON' | translate }}
                    </fd-ui-button>
                    <fd-ui-button fill="text" size="sm" (click)="close('ProductList')">
                        {{ 'PRODUCT_DETAIL.GO_TO_PRODUCT_LIST_BUTTON' | translate }}
                    </fd-ui-button>
                </div>
            </div>
        </fd-ui-dialog>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductSaveSuccessDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ProductSaveSuccessDialogComponent, RedirectAction>);
    private readonly data = inject(FD_UI_DIALOG_DATA) as ProductSaveSuccessDialogData;

    public get titleKey(): string {
        return this.data.isEdit ? 'PRODUCT_DETAIL.EDIT_SUCCESS' : 'PRODUCT_DETAIL.CREATE_SUCCESS';
    }

    public close(action: RedirectAction): void {
        this.dialogRef.close(action);
    }
}
