import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ChartData, ChartOptions, ChartTypeRegistry, TooltipItem } from 'chart.js';
import { CHART_COLORS } from '../../../constants/chart-colors';
import { NutrientChartData } from '../../../types/charts.data';

@Component({
    selector: 'app-nutrients-summary',
    imports: [
        BaseChartDirective,
        DecimalPipe,
        TranslatePipe
    ],
    templateUrl: './nutrients-summary.component.html',
    styleUrl: './nutrients-summary.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NutrientsSummaryComponent {
    public readonly CHART_COLORS = CHART_COLORS;

    private readonly translateService = inject(TranslateService);

    public calories = input.required<number>();
    public nutrientChartData = input.required<NutrientChartData>();

    public get nutrientsPieChartData(): ChartData<'pie', number[], string> {
        return {
            labels: [
                this.translateService.instant('NUTRIENTS.PROTEINS'),
                this.translateService.instant('NUTRIENTS.FATS'),
                this.translateService.instant('NUTRIENTS.CARBS'),
            ],
            datasets: [
                {
                    data: [
                        this.nutrientChartData().proteins,
                        this.nutrientChartData().fats,
                        this.nutrientChartData().carbs,
                    ],
                    backgroundColor: [
                        CHART_COLORS.proteins,
                        CHART_COLORS.fats,
                        CHART_COLORS.carbs,
                    ],
                },
            ],
        };
    }

    public get nutrientsBarChartData(): ChartData<'bar', number[], string> {
        return {
            labels: [
                this.translateService.instant('NUTRIENTS.PROTEINS'),
                this.translateService.instant('NUTRIENTS.FATS'),
                this.translateService.instant('NUTRIENTS.CARBS'),
            ],
            datasets: [
                {
                    data: [
                        this.nutrientChartData().proteins,
                        this.nutrientChartData().fats,
                        this.nutrientChartData().carbs,
                    ],
                    backgroundColor: [
                        CHART_COLORS.proteins,
                        CHART_COLORS.fats,
                        CHART_COLORS.carbs,
                    ],
                },
            ],
        };
    }

    public baseNutrientsChartOptions = {
        responsive: true,
        plugins: {
            tooltip: {
                callbacks: {
                    label: (context: TooltipItem<any>): string => this.getFormattedTooltip(context),
                }
            }
        }
    };

    public pieChartOptions: ChartOptions<'pie'> = {
        ...this.baseNutrientsChartOptions
    };

    public barChartOptions: ChartOptions<'bar'> = {
        ...this.baseNutrientsChartOptions
    }

    private getFormattedTooltip<T extends keyof ChartTypeRegistry>(context: TooltipItem<T>): string {
        const label = context.label || '';
        const value = Number(context.raw) || 0;
        const formattedValue = parseFloat(value.toFixed(2));

        return `${label}: ${formattedValue} ${this.translateService.instant('STATISTICS.GRAMS')}`;
    }
}
