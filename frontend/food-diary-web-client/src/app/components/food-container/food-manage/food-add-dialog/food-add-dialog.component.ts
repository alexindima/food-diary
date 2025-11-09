import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseFoodManageComponent } from '../base-food-manage.component';
import { injectContext } from '@taiga-ui/polymorpheus';
import {
    TuiButton,
    TuiDialogContext,
    TuiError,
    TuiIcon,
    TuiLabel,
    TuiTextfieldComponent, TuiTextfieldDirective
} from '@taiga-ui/core';
import { Product } from '../../../../types/product.data';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiFieldErrorPipe } from '@taiga-ui/kit';
import { AsyncPipe } from '@angular/common';
import {
    TuiInputNumberModule,
    TuiSelectModule,
    TuiTextfieldControllerModule
} from '@taiga-ui/legacy';
import {
    NutrientsSummaryComponent
} from '../../../shared/nutrients-summary/nutrients-summary.component';
import { CustomGroupComponent } from '../../../shared/custom-group/custom-group.component';
import { ZXingScannerModule } from '@zxing/ngx-scanner';

@Component({
    selector: 'fd-food-add-dialog',
    templateUrl: '../base-food-manage.component.html',
    styleUrls: ['./food-add-dialog.component.less', '../base-food-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiLabel,
        TuiError,
        TuiFieldErrorPipe,
        AsyncPipe,
        TuiButton,
        TuiSelectModule,
        TuiTextfieldControllerModule,
        TuiTextfieldComponent,
        TuiTextfieldDirective,
        TuiInputNumberModule,
        NutrientsSummaryComponent,
        CustomGroupComponent,
        ZXingScannerModule,
        TuiIcon,
    ]
})
export class FoodAddDialogComponent extends BaseFoodManageComponent {
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
                },
                info: {
                    lineStyles: {
                        calories: {
                            fontSize: 16
                        }
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
}
