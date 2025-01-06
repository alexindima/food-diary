import {
    ChangeDetectionStrategy,
    Component,
    HostListener,
    inject,
    Input,
    input,
    OnInit
} from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { DecimalPipe, NgStyle } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ChartData, ChartOptions, ChartTypeRegistry, TooltipItem } from 'chart.js';
import { CHART_COLORS } from '../../../constants/chart-colors';
import { NutrientChartData } from '../../../types/charts.data';

@Component({
    selector: 'app-nutrients-summary',
    imports: [
        BaseChartDirective,
        DecimalPipe,
        TranslatePipe,
        NgStyle,
    ],
    templateUrl: './nutrients-summary.component.html',
    styleUrl: './nutrients-summary.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NutrientsSummaryComponent implements OnInit {
    public readonly CHART_COLORS = CHART_COLORS;

    private readonly translateService = inject(TranslateService);

    public calories = input.required<number>();
    public nutrientChartData = input.required<NutrientChartData>();

    @Input() public config: NutrientsSummaryConfig = {
        styles: {
            common: {
                infoBreakpoints: {
                    columnLayout: 600,
                    chartBlockSize: 256,
                    gap: 12
                },
                gap: 16
            },
            charts: {
                chartBlockSize: 192,
                gap: 16,
                breakpoints: {
                    columnLayout: 768,
                    chartBlockSize: 192,
                    gap: 12,
                },
            },
            info: {
                lineStyles: {
                    calories: {
                        fontSize: 24,
                        lineHeight: 28,
                    },
                    nutrients: {
                        fontSize: 16,
                        lineHeight: 20,
                    },
                },
            },
        },
        content: {
            hideBarChart: false,
            hidePieChart: false,
        },
    };

    public isColumnLayout = false;
    public areChartsBelowInfo = false;

    @HostListener('window:resize', ['$event'])
    public onResize(event: UIEvent): void {
        const width = (event.target as Window).innerWidth;
        this.updateLayout(width);
    }

    public ngOnInit(): void {

        this.updateLayout(window.innerWidth);
    }

    private updateLayout(screenWidth: number): void {
        this.isColumnLayout = screenWidth <= this.config.styles.charts.breakpoints.columnLayout;
        this.areChartsBelowInfo = screenWidth <= this.config.styles.common.infoBreakpoints.columnLayout;
    }

    public get summaryWrapperStyles(): object {
        const gapValue = this.isColumnLayout
            ? this.config.styles.common.infoBreakpoints.gap
            : this.config.styles.common.gap;

        return { gap: `${gapValue}px`};
    }

    public get calorieStyles(): object {
        const { fontSize, lineHeight } = this.config.styles.info.lineStyles.calories;
        return {
            fontSize: `${fontSize}px`,
            lineHeight: `${lineHeight}px`,
        };
    }

    public get nutrientStyles(): object {
        const { fontSize, lineHeight } = this.config.styles.info.lineStyles.nutrients;
        return {
            fontSize: `${fontSize}px`,
            lineHeight: `${lineHeight}px`,
        };
    }

    public get nutrientColorStyles(): object {
        const fontSize = this.config.styles.info.lineStyles.nutrients.fontSize;
        return {
            height: `${fontSize}px`,
            width: `${fontSize * 2}px`,
        };
    }

    public get chartsWrapperStyles(): object {
        const gapValue = this.isColumnLayout
            ? this.config.styles.charts.breakpoints.gap
            : this.config.styles.charts.gap;

        return {
            gap: `${gapValue}px`,
        };
    }

    public get chartsBlockSize(): number {
        return this.areChartsBelowInfo
            ? this.config.styles.common.infoBreakpoints.chartBlockSize
            : this.isColumnLayout
                ? this.config.styles.charts.breakpoints.chartBlockSize
                : this.config.styles.charts.chartBlockSize;
    }

    public get chartStyles(): object {
        return {
            width: `${this.chartsBlockSize}px`,
            height: `${this.chartsBlockSize}px`,
        };
    }

    public get chartCanvasStyles(): object {
        return {
            maxWidth: `${this.chartsBlockSize}px`,
            maxHeight: `${this.chartsBlockSize}px`,
        };
    }

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

export interface NutrientsSummaryConfig {
    styles: {
        common: {
            gap: 16,
            infoBreakpoints: {
                columnLayout: number;
                chartBlockSize: number;
                gap: number;
            };
        },
        charts: {
            chartBlockSize: number;
            gap: number;
            breakpoints: {
                columnLayout: number;
                chartBlockSize: number;
                gap: number;
            };
        },
        info: {
            lineStyles: {
                calories: {
                    fontSize: number;
                    lineHeight: number;
                };
                nutrients: {
                    fontSize: number;
                    lineHeight: number;
                };
            };
        };
    };
    content: {
        hideBarChart: boolean;
        hidePieChart: boolean;
    };
}
