import {
    ChangeDetectionStrategy,
    Component,
    inject,
    TemplateRef,
    ViewChild
} from '@angular/core';
import { TuiButton, TuiDialogContext, TuiDialogService } from '@taiga-ui/core';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';
import { Consumption } from '../../../types/consumption.data';
import { DatePipe } from '@angular/common';
import {
    NutrientsSummaryComponent, NutrientsSummaryConfig
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { NutrientChartData } from '../../../types/charts.data';

@Component({
    selector: 'app-consumption-detail',
    templateUrl: './consumption-detail.component.html',
    styleUrls: ['./consumption-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, DatePipe, TuiButton, NutrientsSummaryComponent]
})
export class ConsumptionDetailComponent {
    public readonly context = injectContext<TuiDialogContext<ConsumptionDetailActionResult, Consumption>>();
    private readonly dialogService = inject(TuiDialogService);

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<boolean, void>>;

    public readonly consumption: Consumption;
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

    public constructor() {
        this.consumption = this.context.data;

        this.calories = this.calculateNutrientTotal('caloriesPerBase');
        this.nutrientChartData = {
            proteins: this.calculateNutrientTotal('proteinsPerBase'),
            fats: this.calculateNutrientTotal('fatsPerBase'),
            carbs: this.calculateNutrientTotal('carbsPerBase'),
        };
    }
    public onEdit(): void {
        const editResult = new ConsumptionDetailActionResult(this.consumption.id, 'Edit');
        this.context.completeWith(editResult);
    }

    public onDelete(): void {
        this.showConfirmDialog();
    }

    private showConfirmDialog(): void {
        this.dialogService
            .open(this.confirmDialog, {
                dismissible: true,
                appearance: 'without-border-radius',
            })
            .subscribe(confirm => {
                if (confirm) {
                    const deleteResult = new ConsumptionDetailActionResult(this.consumption.id, 'Delete');
                    this.context.completeWith(deleteResult);
                }
            });
    }

    private calculateNutrientTotal(nutrientKey: NutrientType): number {
        return this.consumption.items.reduce((sum, item) =>
            sum + ((item.food?.[nutrientKey] ?? 0) * item.amount) / 100, 0);
    }
}

class ConsumptionDetailActionResult {
    public constructor(
        public id: number,
        public action: ConsumptionDetailAction,
    ) {}
}

type ConsumptionDetailAction = 'Edit' | 'Delete';

type NutrientType = 'caloriesPerBase' | 'proteinsPerBase' | 'fatsPerBase' | 'carbsPerBase';
