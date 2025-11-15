import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseProductManageComponent } from '../base-product-manage.component';
import { injectContext } from '@taiga-ui/polymorpheus';
import {
    TuiButton,
    TuiDialogContext,
    TuiError,
} from '@taiga-ui/core';
import { Product } from '../../../../types/product.data';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiFieldErrorPipe } from '@taiga-ui/kit';
import { AsyncPipe, DecimalPipe } from '@angular/common';
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
import { FdUiRadioGroupComponent } from '../../../../ui-kit/radio/fd-ui-radio-group.component';

@Component({
    selector: 'fd-product-add-dialog',
    templateUrl: '../base-product-manage.component.html',
    styleUrls: ['./product-add-dialog.component.less', '../base-product-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiError,
        TuiFieldErrorPipe,
        AsyncPipe,
        DecimalPipe,
        TuiButton,
        TuiSelectModule,
        TuiTextfieldControllerModule,
        TuiInputNumberModule,
        NutrientsSummaryComponent,
        ZXingScannerModule,
        FdUiInputComponent,
        FdUiCardComponent,
        FdUiRadioGroupComponent,
    ]
})
export class ProductAddDialogComponent extends BaseProductManageComponent {
    public readonly context = injectContext<TuiDialogContext<Product, null>>();

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
            this.context.completeWith(product);
        }

        return product;
    }

    public override async onCancel(): Promise<void> {
        this.context.completeWith(this.context.data as Product);
    }
}
