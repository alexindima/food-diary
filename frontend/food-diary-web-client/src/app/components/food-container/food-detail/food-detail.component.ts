import { ChangeDetectionStrategy, Component, inject, TemplateRef, ViewChild } from '@angular/core';
import { Food } from '../../../types/food.data';
import { TuiButton, TuiDialogContext, TuiDialogService } from '@taiga-ui/core';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';
import {
    NutrientsSummaryComponent,
    NutrientsSummaryConfig
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { NutrientChartData } from '../../../types/charts.data';

@Component({
    selector: 'fd-food-detail',
    templateUrl: './food-detail.component.html',
    styleUrls: ['./food-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, TuiButton, NutrientsSummaryComponent]
})
export class FoodDetailComponent {
    public readonly context = injectContext<TuiDialogContext<FoodDetailActionResult, Food>>();
    private readonly dialogService = inject(TuiDialogService);

    public food: Food;

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

    public get isActionDisabled(): boolean {
        return this.food.usageCount > 0;
    }

    public constructor() {
        this.food = this.context.data;

        this.calories = this.food.caloriesPerBase;
        this.nutrientChartData = {
            proteins: this.food.proteinsPerBase,
            fats: this.food.fatsPerBase,
            carbs: this.food.carbsPerBase,
        };
    }

    public onEdit(): void {
        const editResult = new FoodDetailActionResult(this.food.id, 'Edit');
        this.context.completeWith(editResult);
    }

    public onDelete(): void {
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
                    const deleteResult = new FoodDetailActionResult(this.food.id, 'Delete');
                    this.context.completeWith(deleteResult);
                }
            });
    }
}

class FoodDetailActionResult {
    public constructor(
        public id: number,
        public action: FoodDetailAction,
    ) {}
}

type FoodDetailAction = 'Edit' | 'Delete';
