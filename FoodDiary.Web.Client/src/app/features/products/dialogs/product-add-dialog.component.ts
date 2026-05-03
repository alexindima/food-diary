import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';

import { ManageHeaderComponent } from '../../../components/shared/manage-header/manage-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { BaseProductManageComponent } from '../components/manage/base-product-manage.component';
import { ProductBasicInfoComponent } from '../components/manage/product-basic-info/product-basic-info.component';
import { ProductNutritionEditorComponent } from '../components/manage/product-nutrition-editor/product-nutrition-editor.component';
import { type Product } from '../models/product.data';

@Component({
    selector: 'fd-product-add-dialog',
    templateUrl: '../components/manage/base-product-manage.component.html',
    styleUrls: ['./product-add-dialog.component.scss', '../components/manage/base-product-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        FdPageContainerDirective,
        ManageHeaderComponent,
        ProductBasicInfoComponent,
        ProductNutritionEditorComponent,
    ],
})
export class ProductAddDialogComponent extends BaseProductManageComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ProductAddDialogComponent, Product | null>);
    private readonly initialProduct = inject(FD_UI_DIALOG_DATA, { optional: true }) as Product | null;

    public constructor() {
        super();

        this.skipConfirmDialog = true;
    }

    public override async onSubmit(): Promise<Product | null> {
        const product = await super.onSubmit();
        if (product) {
            this.dialogRef.close(product);
        }

        return product;
    }

    public override async onCancel(): Promise<void> {
        this.dialogRef.close(this.initialProduct ?? null);
    }
}
