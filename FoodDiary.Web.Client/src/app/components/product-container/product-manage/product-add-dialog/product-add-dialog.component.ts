import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { BaseProductManageComponent } from '../base-product-manage.component';
import { Product } from '../../../../types/product.data';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { PageHeaderComponent } from '../../../shared/page-header/page-header.component';
import { BaseChartDirective } from 'ng2-charts';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';

@Component({
    selector: 'fd-product-add-dialog',
    templateUrl: '../base-product-manage.component.html',
    styleUrls: ['./product-add-dialog.component.scss', '../base-product-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        ZXingScannerModule,
        FdUiInputComponent,
        FdUiCardComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        PageHeaderComponent,
        BaseChartDirective,
        FdPageContainerDirective,
    ]
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
