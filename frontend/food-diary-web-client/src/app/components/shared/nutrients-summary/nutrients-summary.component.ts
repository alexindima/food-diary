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
import { RecursivePartial } from '../../../types/common.data';
import { CustomGroupComponent } from '../custom-group/custom-group.component';

@Component({
    selector: 'fd-nutrients-summary',
    imports: [
        BaseChartDirective,
        DecimalPipe,
        TranslatePipe,
        NgStyle,
        CustomGroupComponent,
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

    @Input() public config: NutrientsSummaryConfig = {};
    public mergedConfig!: NutrientsSummaryConfigInternal;

    public isColumnLayout = false;
    public areChartsBelowInfo = false;

    @HostListener('window:resize', ['$event'])
    public onResize(event: UIEvent): void {
        const width = (event.target as Window).innerWidth;
        this.updateLayout(width);
    }

    public ngOnInit(): void {
        this.mergedConfig = this.mergeConfig(this.config);
        this.updateLayout(window.innerWidth);
    }

    private updateLayout(screenWidth: number): void {
        this.isColumnLayout = screenWidth <= this.mergedConfig.styles.charts.breakpoints.columnLayout;
        this.areChartsBelowInfo = screenWidth <= this.mergedConfig.styles.common.infoBreakpoints.columnLayout;
    }

    public get summaryWrapperStyles(): object {
        const gapValue = this.isColumnLayout
            ? this.mergedConfig.styles.common.infoBreakpoints.gap
            : this.mergedConfig.styles.common.gap;

        return { gap: `${gapValue}px`};
    }

    public get calorieStyles(): object {
        const { fontSize, lineHeight } = this.mergedConfig.styles.info.lineStyles.calories;
        return {
            fontSize: `${fontSize}px`,
            lineHeight: `${lineHeight}px`,
        };
    }

    public get nutrientStyles(): object {
        const { fontSize, lineHeight } = this.mergedConfig.styles.info.lineStyles.nutrients;
        return {
            fontSize: `${fontSize}px`,
            lineHeight: `${lineHeight}px`,
        };
    }

    public get nutrientColorStyles(): object {
        const fontSize = this.mergedConfig.styles.info.lineStyles.nutrients.fontSize;
        return {
            height: `${fontSize}px`,
            width: `${fontSize * 2}px`,
        };
    }

    public get chartsWrapperStyles(): object {
        const gapValue = this.isColumnLayout
            ? this.mergedConfig.styles.charts.breakpoints.gap
            : this.mergedConfig.styles.charts.gap;

        return {
            gap: `${gapValue}px`,
        };
    }

    public get chartsBlockSize(): number {
        return this.areChartsBelowInfo
            ? this.mergedConfig.styles.common.infoBreakpoints.chartBlockSize
            : this.isColumnLayout
                ? this.mergedConfig.styles.charts.breakpoints.chartBlockSize
                : this.mergedConfig.styles.charts.chartBlockSize;
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

    private mergeConfig(userConfig: Partial<NutrientsSummaryConfig>): NutrientsSummaryConfigInternal {
        return {
            ...DEFAULT_CONFIG,
            ...userConfig,
            styles: {
                ...DEFAULT_CONFIG.styles,
                ...userConfig.styles,
                common: {
                    ...DEFAULT_CONFIG.styles.common,
                    ...userConfig.styles?.common,
                    infoBreakpoints: {
                        ...DEFAULT_CONFIG.styles.common.infoBreakpoints,
                        ...userConfig.styles?.common?.infoBreakpoints,
                    },
                },
                charts: {
                    ...DEFAULT_CONFIG.styles.charts,
                    ...userConfig.styles?.charts,
                    breakpoints: {
                        ...DEFAULT_CONFIG.styles.charts.breakpoints,
                        ...userConfig.styles?.charts?.breakpoints,
                    },
                },
                info: {
                    ...DEFAULT_CONFIG.styles.info,
                    ...userConfig.styles?.info,
                    lineStyles: {
                        ...DEFAULT_CONFIG.styles.info.lineStyles,
                        ...userConfig.styles?.info?.lineStyles,
                        calories: {
                            ...DEFAULT_CONFIG.styles.info.lineStyles.calories,
                            ...userConfig.styles?.info?.lineStyles?.calories,
                        },
                        nutrients: {
                            ...DEFAULT_CONFIG.styles.info.lineStyles.nutrients,
                            ...userConfig.styles?.info?.lineStyles?.nutrients,
                        },
                    },
                },
            },
            content: {
                ...DEFAULT_CONFIG.content,
                ...userConfig.content,
            },
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

interface NutrientsSummaryConfigInternal {
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

export type NutrientsSummaryConfig = RecursivePartial<NutrientsSummaryConfigInternal>;

const DEFAULT_CONFIG: NutrientsSummaryConfigInternal = {
    styles: {
        common: {
            gap: 16,
            infoBreakpoints: {
                columnLayout: 600,
                chartBlockSize: 256,
                gap: 12,
            },
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
                    fontSize: 20,
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
