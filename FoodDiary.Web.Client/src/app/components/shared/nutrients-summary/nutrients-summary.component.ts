import { ChangeDetectionStrategy, Component, HostListener, computed, inject, input, signal } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { DecimalPipe, NgStyle, NgTemplateOutlet } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ChartData, ChartOptions, ChartTypeRegistry, TooltipItem } from 'chart.js';
import { CHART_COLORS } from '../../../constants/chart-colors';
import { NutrientData } from '../../../shared/models/charts.data';
import { RecursivePartial } from '../../../shared/lib/common.data';
import { CustomGroupComponent } from '../custom-group/custom-group.component';

@Component({
    selector: 'fd-nutrients-summary',
    imports: [BaseChartDirective, DecimalPipe, TranslatePipe, NgStyle, CustomGroupComponent, NgTemplateOutlet],
    templateUrl: './nutrients-summary.component.html',
    styleUrl: './nutrients-summary.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutrientsSummaryComponent {
    public readonly CHART_COLORS = CHART_COLORS;

    private readonly translateService = inject(TranslateService);

    public calories = input.required<number>();
    public nutrientChartData = input.required<NutrientData>();
    public fiberValue = input<number | null>(null);
    public fiberUnitKey = input<string>('PRODUCT_AMOUNT_UNITS_SHORT.G');
    public alcoholValue = input<number | null>(null);
    public alcoholUnitKey = input<string>('PRODUCT_AMOUNT_UNITS_SHORT.G');
    public bare = input<boolean>(false);
    public showBarChart = input<boolean>(false);

    public readonly config = input<NutrientsSummaryConfig>({});
    public readonly mergedConfig = computed(() => this.mergeConfig(this.config()));
    private readonly viewportWidth = signal(window.innerWidth);
    public readonly isColumnLayout = computed(() => this.viewportWidth() <= this.mergedConfig().styles.charts.breakpoints.columnLayout);
    public readonly areChartsBelowInfo = computed(
        () => this.viewportWidth() <= this.mergedConfig().styles.common.infoBreakpoints.columnLayout,
    );
    public readonly summaryWrapperStyles = computed(() => {
        const gapValue = this.isColumnLayout()
            ? this.mergedConfig().styles.common.infoBreakpoints.gap
            : this.mergedConfig().styles.common.gap;

        return { gap: `${gapValue}px` };
    });
    public readonly nutrientStyles = computed(() => {
        const { fontSize, lineHeight } = this.mergedConfig().styles.info.lineStyles.nutrients;
        return {
            fontSize: `${fontSize}px`,
            lineHeight: `${lineHeight}px`,
        };
    });
    public readonly nutrientColorStyles = computed(() => {
        const fontSize = this.mergedConfig().styles.info.lineStyles.nutrients.fontSize;
        return {
            height: `${fontSize}px`,
            width: `${fontSize * 2}px`,
        };
    });
    public readonly chartsWrapperStyles = computed(() => {
        const gapValue = this.isColumnLayout() ? this.mergedConfig().styles.charts.breakpoints.gap : this.mergedConfig().styles.charts.gap;

        return {
            gap: `${gapValue}px`,
        };
    });
    public readonly chartsBlockSize = computed(() => {
        if (this.areChartsBelowInfo()) {
            return this.mergedConfig().styles.common.infoBreakpoints.chartBlockSize;
        }

        return this.isColumnLayout()
            ? this.mergedConfig().styles.charts.breakpoints.chartBlockSize
            : this.mergedConfig().styles.charts.chartBlockSize;
    });
    public readonly chartStyles = computed(() => ({
        width: `${this.chartsBlockSize()}px`,
        height: `${this.chartsBlockSize()}px`,
    }));
    public readonly chartCanvasStyles = computed(() => ({
        maxWidth: `${this.chartsBlockSize()}px`,
        maxHeight: `${this.chartsBlockSize()}px`,
    }));
    public readonly hasNutrientData = computed(() => {
        const data = this.nutrientChartData();
        return (data.proteins ?? 0) + (data.fats ?? 0) + (data.carbs ?? 0) > 0;
    });
    public readonly nutrientsPieChartData = computed<ChartData<'pie', number[], string>>(() => ({
        labels: [
            this.translateService.instant('NUTRIENTS.PROTEINS'),
            this.translateService.instant('NUTRIENTS.FATS'),
            this.translateService.instant('NUTRIENTS.CARBS'),
        ],
        datasets: [
            {
                data: [this.nutrientChartData().proteins, this.nutrientChartData().fats, this.nutrientChartData().carbs],
                backgroundColor: [CHART_COLORS.proteins, CHART_COLORS.fats, CHART_COLORS.carbs],
            },
        ],
    }));
    public readonly nutrientsBarChartData = computed<ChartData<'bar', number[], string>>(() => ({
        labels: [
            this.translateService.instant('NUTRIENTS.PROTEINS'),
            this.translateService.instant('NUTRIENTS.FATS'),
            this.translateService.instant('NUTRIENTS.CARBS'),
        ],
        datasets: [
            {
                data: [this.nutrientChartData().proteins, this.nutrientChartData().fats, this.nutrientChartData().carbs],
                backgroundColor: [CHART_COLORS.proteins, CHART_COLORS.fats, CHART_COLORS.carbs],
            },
        ],
    }));

    @HostListener('window:resize', ['$event'])
    public onResize(event: UIEvent): void {
        const width = (event.target as Window).innerWidth;
        this.viewportWidth.set(width);
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

    public baseNutrientsChartOptions = {
        responsive: true,
        plugins: {
            tooltip: {
                callbacks: {
                    label: (context: TooltipItem<'pie' | 'bar'>): string => this.getFormattedTooltip(context),
                },
            },
        },
    };

    public pieChartOptions: ChartOptions<'pie'> = {
        ...this.baseNutrientsChartOptions,
    };

    public barChartOptions: ChartOptions<'bar'> = {
        ...this.baseNutrientsChartOptions,
    };

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
            gap: 16;
            infoBreakpoints: {
                columnLayout: number;
                chartBlockSize: number;
                gap: number;
            };
        };
        charts: {
            chartBlockSize: number;
            gap: number;
            breakpoints: {
                columnLayout: number;
                chartBlockSize: number;
                gap: number;
            };
        };
        info: {
            lineStyles: {
                nutrients: {
                    fontSize: number;
                    lineHeight: number;
                };
            };
        };
    };
    content: {
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
                nutrients: {
                    fontSize: 16,
                    lineHeight: 20,
                },
            },
        },
    },
    content: {
        hidePieChart: false,
    },
};
