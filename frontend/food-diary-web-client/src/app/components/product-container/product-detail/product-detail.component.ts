import { ChangeDetectionStrategy, Component, inject, TemplateRef, ViewChild } from '@angular/core';
import { Product } from '../../../types/product.data';
import { TuiButton, TuiDialogContext, TuiDialogService } from '@taiga-ui/core';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';
import {
    NutrientsSummaryComponent,
    NutrientsSummaryConfig
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { NutrientChartData } from '../../../types/charts.data';

@Component({
    selector: 'fd-product-detail',
    templateUrl: './product-detail.component.html',
    styleUrls: ['./product-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, TuiButton, NutrientsSummaryComponent],
})
export class ProductDetailComponent {
    public readonly context = injectContext<TuiDialogContext<ProductDetailActionResult, Product>>();
    private readonly dialogService = inject(TuiDialogService);

    public product: Product;

    public readonly nutrientSummaryConfig: NutrientsSummaryConfig = {
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

    public calories: number;
    public nutrientChartData: NutrientChartData;

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<boolean, void>>;

    public get isDeleteDisabled(): boolean {
        return !this.product.isOwnedByCurrentUser || this.product.usageCount > 0;
    }

    public get isEditDisabled(): boolean {
        return !this.product.isOwnedByCurrentUser || this.product.usageCount > 0;
    }

    public get warningMessage(): string | null {
        if (!this.isDeleteDisabled && !this.isEditDisabled) {
            return null;
        }

        return this.product.isOwnedByCurrentUser
            ? 'FOOD_DETAIL.WARNING_MESSAGE'
            : 'FOOD_DETAIL.WARNING_NOT_OWNER';
    }

    public constructor() {
        this.product = this.context.data;

        this.calories = this.product.caloriesPerBase;
        this.nutrientChartData = {
            proteins: this.product.proteinsPerBase,
            fats: this.product.fatsPerBase,
            carbs: this.product.carbsPerBase,
        };
    }

    public onEdit(): void {
        if (this.isEditDisabled) {
            return;
        }
        const editResult = new ProductDetailActionResult(this.product.id, 'Edit');
        this.context.completeWith(editResult);
    }

    public onDelete(): void {
        if (this.isDeleteDisabled) {
            return;
        }
        this.showConfirmDialog();
    }

    protected showConfirmDialog(): void {
        this.dialogService
            .open(this.confirmDialog, {
                dismissible: true,
                appearance: 'without-border-radius',
            })
            .subscribe(confirm => {
                if (confirm) {
                    const deleteResult = new ProductDetailActionResult(this.product.id, 'Delete');
                    this.context.completeWith(deleteResult);
                }
            });
    }
}

class ProductDetailActionResult {
    public constructor(
        public id: string,
        public action: ProductDetailAction,
    ) {}
}

type ProductDetailAction = 'Edit' | 'Delete';
