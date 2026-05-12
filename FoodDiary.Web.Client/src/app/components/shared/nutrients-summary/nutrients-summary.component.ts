import { DecimalPipe, NgStyle, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { ChartData, ChartOptions, ChartTypeRegistry, TooltipItem } from 'chart.js';
import { distinctUntilChanged, fromEvent, map } from 'rxjs';

import { CHART_COLORS } from '../../../constants/chart-colors';
import type { RecursivePartial } from '../../../shared/lib/common.data';
import type { NutrientData } from '../../../shared/models/charts.data';
import { CustomGroupComponent } from '../custom-group/custom-group.component';
import { NutrientsSummaryChartsComponent } from './nutrients-summary-charts.component';

const NUTRIENT_COLOR_WIDTH_MULTIPLIER = 2;
const TOOLTIP_DECIMAL_PLACES = 2;

@Component({
    selector: 'fd-nutrients-summary',
    imports: [DecimalPipe, TranslatePipe, NgStyle, CustomGroupComponent, NgTemplateOutlet, NutrientsSummaryChartsComponent],
    templateUrl: './nutrients-summary.component.html',
    styleUrl: './nutrients-summary.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutrientsSummaryComponent {
    public readonly CHART_COLORS = CHART_COLORS;

    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);

    public readonly calories = input.required<number>();
    public readonly nutrientChartData = input.required<NutrientData>();
    public readonly fiberValue = input<number | null>(null);
    public readonly fiberUnitKey = input<string>('PRODUCT_AMOUNT_UNITS_SHORT.G');
    public readonly alcoholValue = input<number | null>(null);
    public readonly alcoholUnitKey = input<string>('PRODUCT_AMOUNT_UNITS_SHORT.G');
    public readonly bare = input<boolean>(false);
    public readonly showBarChart = input<boolean>(false);

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
            width: `${fontSize * NUTRIENT_COLOR_WIDTH_MULTIPLIER}px`,
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
        return data.proteins + data.fats + data.carbs > 0;
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

    public constructor() {
        fromEvent(window, 'resize')
            .pipe(
                map(() => window.innerWidth),
                distinctUntilChanged(),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(width => {
                this.viewportWidth.set(width);
            });
    }

    private mergeConfig(userConfig: Partial<NutrientsSummaryConfig>): NutrientsSummaryConfigInternal {
        const styles = userConfig.styles;
        const commonStyles = styles?.common;
        const infoBreakpoints = commonStyles?.infoBreakpoints;
        const chartStyles = styles?.charts;
        const chartBreakpoints = chartStyles?.breakpoints;
        const infoStyles = styles?.info;
        const nutrientLineStyles = infoStyles?.lineStyles?.nutrients;

        return {
            ...DEFAULT_CONFIG,
            ...userConfig,
            styles: {
                ...DEFAULT_CONFIG.styles,
                ...styles,
                common: {
                    ...DEFAULT_CONFIG.styles.common,
                    ...commonStyles,
                    infoBreakpoints: {
                        ...DEFAULT_CONFIG.styles.common.infoBreakpoints,
                        ...infoBreakpoints,
                    },
                },
                charts: {
                    ...DEFAULT_CONFIG.styles.charts,
                    ...chartStyles,
                    breakpoints: {
                        ...DEFAULT_CONFIG.styles.charts.breakpoints,
                        ...chartBreakpoints,
                    },
                },
                info: {
                    ...DEFAULT_CONFIG.styles.info,
                    ...infoStyles,
                    lineStyles: {
                        nutrients: {
                            ...DEFAULT_CONFIG.styles.info.lineStyles.nutrients,
                            ...nutrientLineStyles,
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
        const label = context.label.length > 0 ? context.label : '';
        const rawValue = Number(context.raw);
        const value = Number.isNaN(rawValue) ? 0 : rawValue;
        const formattedValue = parseFloat(value.toFixed(TOOLTIP_DECIMAL_PLACES));

        return `${label}: ${formattedValue} ${this.translateService.instant('STATISTICS.GRAMS')}`;
    }
}

interface NutrientsSummaryConfigInternal {
    styles: {
        common: {
            gap: number;
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
