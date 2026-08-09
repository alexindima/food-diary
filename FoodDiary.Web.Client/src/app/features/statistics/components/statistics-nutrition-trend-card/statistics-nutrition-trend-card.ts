import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    type FdUiBarChartCategory,
    FdUiBarChartComponent,
    type FdUiBarChartLayout,
    type FdUiBarChartReferenceLine,
    FdUiCardComponent,
    FdUiIconComponent,
    FdUiLineChartComponent,
    type FdUiLineChartPoint,
    type FdUiLineChartReferenceLine,
    type FdUiLineChartSeries,
    FdUiSelectComponent,
    type FdUiSelectOption,
} from 'fd-ui-kit';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import type { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs';
import { merge, startWith } from 'rxjs';

import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import type { StatisticsNutritionDay, StatisticsTrendInsight } from '../../models/statistics-dashboard-card.models';

const PROTEIN_CALORIES_PER_GRAM = 4;
const FAT_CALORIES_PER_GRAM = 9;
const FIBER_CALORIES_PER_GRAM = 2;
const PERCENT_MAX = 100;
const CHART_TICK_COUNT = 5;
const CHART_STEP = 500;
const DECIMAL_RADIX = 10;
const MIN_NUTRIENT_CHART_MAXIMUM = 20;
const MAX_NICE_NUTRIENT_TICK_FACTOR = 10;
const LARGE_RANGE_FRACTIONAL_TICK_FACTOR = 7.5;
const LARGE_RANGE_MINIMUM_MAGNITUDE = 10;
// eslint-disable-next-line @typescript-eslint/no-magic-numbers -- Standard nice-number scale factors.
const NICE_NUTRIENT_TICK_FACTORS = [1, 1.5, 2, 2.5, 5, 7.5, 10] as const;

type TrendDayView = StatisticsNutritionDay & {
    nutrientBars: readonly TrendNutrientBar[];
    distributionBars: readonly TrendNutrientBar[];
};

type TrendNutrientBar = {
    key: 'protein' | 'fat' | 'carbs' | 'fiber';
    value: number;
    height: number;
};

type StatisticsTrendChartMode = 'bars' | 'line';

@Component({
    selector: 'fd-statistics-nutrition-trend-card',
    imports: [
        DecimalPipe,
        TranslatePipe,
        FdUiBarChartComponent,
        FdUiCardComponent,
        FdUiIconComponent,
        FdUiLineChartComponent,
        FdUiSelectComponent,
        FdUiSegmentedToggleComponent,
    ],
    templateUrl: './statistics-nutrition-trend-card.html',
    styleUrl: './statistics-nutrition-trend-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsNutritionTrendCardComponent {
    private readonly translateService = inject(TranslateService);
    private readonly translationChange = toSignal(
        merge(this.translateService.onLangChange, this.translateService.onTranslationChange).pipe(startWith(null)),
        { initialValue: null },
    );

    public readonly tabs = input.required<FdUiTab[]>();
    public readonly selectedTab = input.required<string>();
    public readonly days = input.required<readonly StatisticsNutritionDay[]>();
    public readonly calorieGoal = input.required<number>();
    public readonly insights = input.required<readonly StatisticsTrendInsight[]>();
    public readonly selectedTabChange = output<string>();

    protected readonly chartMode = signal<StatisticsTrendChartMode>('bars');
    protected readonly chartModeOptions = computed<Array<FdUiSelectOption<StatisticsTrendChartMode>>>(() => {
        this.translationChange();
        return [
            { value: 'bars', label: this.translateService.instant('STATISTICS.DASHBOARD.TREND.CHART_BARS') },
            { value: 'line', label: this.translateService.instant('STATISTICS.DASHBOARD.TREND.CHART_LINE') },
        ];
    });
    private readonly chartNumberFormatter = computed(() => {
        this.translationChange();
        return new Intl.NumberFormat(resolveAppLocale(this.translateService.getCurrentLang()), { maximumFractionDigits: 0 });
    });

    protected readonly maxCalories = computed(() => {
        const maximum = Math.max(this.calorieGoal(), ...this.days().map(day => day.calories ?? 0), CHART_STEP);
        return Math.ceil(maximum / CHART_STEP) * CHART_STEP;
    });
    protected readonly maxNutrient = computed(() => {
        const maximum = Math.max(...this.days().flatMap(day => [day.protein, day.fat, day.carbs, day.fiber]), MIN_NUTRIENT_CHART_MAXIMUM);
        const rawTickStep = maximum / (CHART_TICK_COUNT - 1);
        const magnitude = DECIMAL_RADIX ** Math.floor(Math.log10(rawTickStep));
        const normalizedStep = rawTickStep / magnitude;
        const factor =
            NICE_NUTRIENT_TICK_FACTORS.find(
                candidate =>
                    candidate >= normalizedStep &&
                    (candidate !== LARGE_RANGE_FRACTIONAL_TICK_FACTOR || magnitude >= LARGE_RANGE_MINIMUM_MAGNITUDE),
            ) ?? MAX_NICE_NUTRIENT_TICK_FACTOR;
        return factor * magnitude * (CHART_TICK_COUNT - 1);
    });
    protected readonly chartMaximum = computed(() => {
        if (this.selectedTab() === 'macros') {
            return this.maxNutrient();
        }
        if (this.selectedTab() === 'distribution') {
            return PERCENT_MAX;
        }
        return this.maxCalories();
    });
    protected readonly ticks = computed(() =>
        Array.from({ length: CHART_TICK_COUNT }, (_, index) =>
            Math.round(this.chartMaximum() * ((CHART_TICK_COUNT - index - 1) / (CHART_TICK_COUNT - 1))),
        ),
    );
    protected readonly dayViews = computed<readonly TrendDayView[]>(() =>
        this.days().map(day => {
            const nutrientValues = this.getNutrientValues(day);
            const distributionTotal = nutrientValues.reduce((sum, item) => sum + this.toNutrientCalories(item), 0);
            return {
                ...day,
                nutrientBars: nutrientValues.map(item => ({
                    ...item,
                    height: this.toPercent(item.value, this.maxNutrient()),
                })),
                distributionBars: nutrientValues.map(item => ({
                    ...item,
                    height: this.toPercent(this.toNutrientCalories(item), distributionTotal),
                })),
            };
        }),
    );
    protected readonly barChartLayout = computed<FdUiBarChartLayout>(() => {
        if (this.selectedTab() === 'macros') {
            return 'grouped';
        }
        if (this.selectedTab() === 'distribution') {
            return 'stacked';
        }
        return 'single';
    });
    protected readonly barChartCategories = computed<readonly FdUiBarChartCategory[]>(() => {
        this.translationChange();
        const nutrientLabels = {
            protein: this.translateService.instant('STATISTICS.DASHBOARD.NUTRIENTS.PROTEIN'),
            fat: this.translateService.instant('STATISTICS.DASHBOARD.NUTRIENTS.FAT'),
            carbs: this.translateService.instant('STATISTICS.DASHBOARD.NUTRIENTS.CARBS'),
            fiber: this.translateService.instant('STATISTICS.DASHBOARD.NUTRIENTS.FIBER'),
        };
        const colors = {
            protein: 'var(--fd-color-primary-500)',
            fat: 'var(--fd-color-orange-500)',
            carbs: 'var(--fd-color-sky-500)',
            fiber: 'var(--fd-color-rose-500)',
        };

        return this.dayViews().map(day => {
            if (this.selectedTab() === 'calories') {
                return {
                    label: day.label,
                    ariaLabel: `${day.label}: ${day.calories ?? 0}`,
                    values: [
                        {
                            label: this.translateService.instant('GENERAL.NUTRIENTS.CALORIES'),
                            value: day.calories,
                            color: colors.protein,
                        },
                    ],
                };
            }

            const nutrients = this.selectedTab() === 'distribution' ? day.distributionBars : day.nutrientBars;
            return {
                label: day.label,
                ariaLabel: day.label,
                values: nutrients.map(item => ({
                    label: nutrientLabels[item.key],
                    value: day.calories === null ? null : this.selectedTab() === 'distribution' ? item.height : item.value,
                    color: colors[item.key],
                })),
            };
        });
    });
    protected readonly barChartReferenceLines = computed<readonly FdUiBarChartReferenceLine[]>(() => {
        if (this.selectedTab() !== 'calories' || this.calorieGoal() <= 0) {
            return [];
        }
        return this.calorieReferenceLines().map(line => ({ value: line.value, label: line.label, color: line.color }));
    });
    protected readonly calorieLinePoints = computed<readonly FdUiLineChartPoint[]>(() =>
        this.days().map(day => ({ label: day.label, value: day.calories })),
    );
    protected readonly calorieReferenceLines = computed<readonly FdUiLineChartReferenceLine[]>(() => {
        this.translationChange();
        if (this.selectedTab() !== 'calories' || this.calorieGoal() <= 0) {
            return [];
        }
        return [
            {
                value: this.calorieGoal(),
                label: this.translateService.instant('STATISTICS.DASHBOARD.TREND.GOAL', {
                    value: this.formatChartNumber(this.calorieGoal()),
                }),
                color: 'var(--fd-color-text-subtle)',
                lineStyle: 'dashed',
                outOfRangeBehavior: 'hide',
            },
        ];
    });
    protected readonly nutrientLineSeries = computed<readonly FdUiLineChartSeries[]>(() => {
        this.translationChange();
        const definitions = [
            { key: 'protein' as const, labelKey: 'STATISTICS.DASHBOARD.NUTRIENTS.PROTEIN', color: 'var(--fd-color-primary-500)' },
            { key: 'fat' as const, labelKey: 'STATISTICS.DASHBOARD.NUTRIENTS.FAT', color: 'var(--fd-color-orange-500)' },
            { key: 'carbs' as const, labelKey: 'STATISTICS.DASHBOARD.NUTRIENTS.CARBS', color: 'var(--fd-color-sky-500)' },
            { key: 'fiber' as const, labelKey: 'STATISTICS.DASHBOARD.NUTRIENTS.FIBER', color: 'var(--fd-color-rose-500)' },
        ];
        return definitions.map(definition => ({
            label: this.translateService.instant(definition.labelKey),
            color: definition.color,
            fillColor: 'transparent',
            points: this.days().map(day => ({ label: day.label, value: day[definition.key] })),
        }));
    });
    protected readonly distributionLineSeries = computed<readonly FdUiLineChartSeries[]>(() =>
        this.nutrientLineSeries().map((series, index) => ({
            ...series,
            points: this.days().map(day => {
                const nutrients = this.getNutrientValues(day);
                const total = nutrients.reduce((sum, item) => sum + this.toNutrientCalories(item), 0);
                const nutrient = nutrients[index];
                return {
                    label: day.label,
                    value: this.toPercent(this.toNutrientCalories(nutrient), total),
                };
            }),
        })),
    );
    protected readonly selectedLineSeries = computed<readonly FdUiLineChartSeries[]>(() => {
        if (this.selectedTab() === 'macros') {
            return this.nutrientLineSeries();
        }
        if (this.selectedTab() === 'distribution') {
            return this.distributionLineSeries();
        }
        return [];
    });

    protected readonly chartUnitKey = computed(() => {
        if (this.selectedTab() === 'macros') {
            return 'GENERAL.UNITS.G';
        }
        if (this.selectedTab() === 'distribution') {
            return 'STATISTICS.DASHBOARD.TREND.PERCENT_UNIT';
        }
        return 'GENERAL.UNITS.KCAL';
    });

    protected onTabChange(value: string): void {
        this.selectedTabChange.emit(value);
    }

    protected changeChartMode(value: StatisticsTrendChartMode | null): void {
        if (value !== null) {
            this.chartMode.set(value);
        }
    }

    protected readonly formatChartNumber = (value: number): string => this.chartNumberFormatter().format(value);

    protected getInsightIcon(insight: StatisticsTrendInsight): string {
        if (insight.key === 'calories') {
            return 'local_fire_department';
        }
        if (insight.key === 'protein') {
            return 'fitness_center';
        }
        if (insight.key === 'fat') {
            return 'water_drop';
        }
        if (insight.key === 'completeness') {
            return 'calendar_month';
        }
        return 'monitoring';
    }

    private getNutrientValues(day: StatisticsNutritionDay): Array<Omit<TrendNutrientBar, 'height'>> {
        return [
            { key: 'protein', value: day.protein },
            { key: 'fat', value: day.fat },
            { key: 'carbs', value: day.carbs },
            { key: 'fiber', value: day.fiber },
        ];
    }

    private toNutrientCalories(item: Omit<TrendNutrientBar, 'height'>): number {
        const factor =
            item.key === 'fat' ? FAT_CALORIES_PER_GRAM : item.key === 'fiber' ? FIBER_CALORIES_PER_GRAM : PROTEIN_CALORIES_PER_GRAM;
        return item.value * factor;
    }

    private toPercent(value: number, maximum: number): number {
        return maximum > 0 ? Math.max(0, (value / maximum) * PERCENT_MAX) : 0;
    }
}
