import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { BaseProductManageComponent } from '../base-product-manage.component';
import { Product } from '../../../../types/product.data';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { DecimalPipe } from '@angular/common';
import {
    TuiInputNumberModule,
    TuiSelectModule,
    TuiTextfieldControllerModule
} from '@taiga-ui/legacy';
import {
    NutrientsSummaryComponent
} from '../../../shared/nutrients-summary/nutrients-summary.component';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { FdUiInputComponent } from '../../../../ui-kit/input/fd-ui-input.component';
import { FdUiCardComponent } from '../../../../ui-kit/card/fd-ui-card.component';
import { FdUiSelectComponent } from '../../../../ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from '../../../../ui-kit/textarea/fd-ui-textarea.component';
import { FdUiButtonComponent } from '../../../../ui-kit/button/fd-ui-button.component';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FdUiFormErrorComponent } from '../../../../ui-kit/form-error/fd-ui-form-error.component';

@Component({
    selector: 'fd-product-add-dialog',
    templateUrl: '../base-product-manage.component.html',
    styleUrls: ['./product-add-dialog.component.less', '../base-product-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        DecimalPipe,
        TuiSelectModule,
        TuiTextfieldControllerModule,
        TuiInputNumberModule,
        NutrientsSummaryComponent,
        ZXingScannerModule,
        FdUiInputComponent,
        FdUiCardComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
    ]
})
export class ProductAddDialogComponent extends BaseProductManageComponent {
    private readonly dialogRef = inject(MatDialogRef<ProductAddDialogComponent, Product | null>);
    private readonly initialProduct = inject(MAT_DIALOG_DATA, { optional: true }) as Product | null;

    public constructor() {
        super();

        this.skipConfirmDialog = true;
        this.nutrientSummaryConfig = {
            styles: {
                common: {
                    infoBreakpoints: {
                        columnLayout: 680
                    }
                },
                charts: {
                    chartBlockSize: 160,
                    breakpoints: {
                        columnLayout: 680
                    }
                }
            }
        };
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
