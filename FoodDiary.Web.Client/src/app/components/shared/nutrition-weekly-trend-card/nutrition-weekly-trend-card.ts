import { formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    type FdUiBarChartCategory,
    FdUiBarChartComponent,
    type FdUiBarChartReferenceLine,
    FdUiIconComponent,
    FdUiSelectComponent,
    type FdUiSelectOption,
} from 'fd-ui-kit';
import { merge, startWith } from 'rxjs';

import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { DashboardWidgetFrameComponent } from '../dashboard-widget-frame/dashboard-widget-frame';

const PROTEIN_CALORIES_PER_GRAM = 4;
const CARB_CALORIES_PER_GRAM = 4;
const FAT_CALORIES_PER_GRAM = 9;
const FIBER_CALORIES_PER_GRAM = 2;
const TREND_TICK_COUNT = 5;
const SHORT_TREND_DAYS = 3;
const DEFAULT_TREND_DAYS = 7;
const CALORIE_SCALE_STEP = 500;

export type NutritionInsightKind =
    'empty' | 'calorie-excess' | 'carb-excess' | 'fat-excess' | 'protein-deficit' | 'fiber-deficit' | 'in-progress' | 'balanced';
export type NutritionInsightMetric = 'calories' | 'proteins' | 'fats' | 'carbs' | 'fiber';
export type NutritionTrendInsight = {
    kind: NutritionInsightKind;
    tone: 'neutral' | 'positive' | 'warning';
    metric?: NutritionInsightMetric;
    current?: number;
    goal?: number;
};
export type NutritionTrendPoint = {
    date: string;
    calories: number;
    proteins?: number;
    fats?: number;
    carbs?: number;
    fiber?: number;
};

type TrendRange = typeof SHORT_TREND_DAYS | typeof DEFAULT_TREND_DAYS;

type TrendPoint = {
    date: string;
    label: string;
    fullDateLabel: string;
    dayLabel: string;
    dateKey: string;
    monthLabel: string;
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    isLatest: boolean;
};

type InsightConfig = {
    titleKey: string;
    hintKey: string;
    icon: string;
};

const INSIGHT_CONFIG: Record<NutritionInsightKind, InsightConfig> = {
    empty: { titleKey: 'EMPTY_TITLE', hintKey: 'EMPTY_HINT', icon: 'restaurant_menu' },
    'calorie-excess': { titleKey: 'CALORIE_EXCESS_TITLE', hintKey: 'CALORIE_EXCESS_HINT', icon: 'trending_up' },
    'carb-excess': { titleKey: 'CARB_EXCESS_TITLE', hintKey: 'CARB_EXCESS_HINT', icon: 'trending_up' },
    'fat-excess': { titleKey: 'FAT_EXCESS_TITLE', hintKey: 'FAT_EXCESS_HINT', icon: 'trending_up' },
    'protein-deficit': { titleKey: 'PROTEIN_DEFICIT_TITLE', hintKey: 'PROTEIN_DEFICIT_HINT', icon: 'fitness_center' },
    'fiber-deficit': { titleKey: 'FIBER_DEFICIT_TITLE', hintKey: 'FIBER_DEFICIT_HINT', icon: 'spa' },
    'in-progress': { titleKey: 'IN_PROGRESS_TITLE', hintKey: 'IN_PROGRESS_HINT', icon: 'schedule' },
    balanced: { titleKey: 'BALANCED_TITLE', hintKey: 'BALANCED_HINT', icon: 'check' },
};

const METRIC_LABEL_KEYS: Record<NutritionInsightMetric, string> = {
    calories: 'GENERAL.CALORIES',
    proteins: 'GENERAL.NUTRIENTS.PROTEIN',
    fats: 'GENERAL.NUTRIENTS.FAT',
    carbs: 'GENERAL.NUTRIENTS.CARB',
    fiber: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
};

@Component({
    selector: 'fd-nutrition-weekly-trend-card',
    imports: [RouterLink, FdUiBarChartComponent, FdUiIconComponent, FdUiSelectComponent, TranslatePipe, DashboardWidgetFrameComponent],
    templateUrl: './nutrition-weekly-trend-card.html',
    styleUrl: './nutrition-weekly-trend-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutritionWeeklyTrendCardComponent {
    private readonly translateService = inject(TranslateService);
    private readonly translationChange = toSignal(
        merge(this.translateService.onLangChange, this.translateService.onTranslationChange).pipe(startWith(null)),
        { initialValue: null },
    );

    public readonly points = input.required<NutritionTrendPoint[]>();
    public readonly dailyGoal = input.required<number>();
    public readonly insight = input.required<NutritionTrendInsight>();
    public readonly isToday = input(true);
    public readonly details = output();
    public readonly interactive = input(false);
    public readonly currentDate = input<Date | null>(null);
    protected readonly currentDateKey = computed(() => {
        const date = this.currentDate();
        return date === null ? null : formatDate(date, 'yyyy-MM-dd', 'en-US');
    });
    protected readonly selectedDate = signal<string | null>(null);
    protected readonly selectedPoint = computed(
        () => this.trendPoints().find(point => point.date === this.selectedDate()) ?? this.trendPoints().at(-1),
    );
    protected selectDay(index: number): void {
        this.selectedDate.set(this.trendPoints()[index]?.date ?? null);
    }
    protected readonly selectedMetrics = computed(() => {
        const point = this.selectedPoint();
        return point === undefined
            ? []
            : (['proteins', 'fats', 'carbs', 'fiber'] as const).map(key => ({ key, label: METRIC_LABEL_KEYS[key], value: point[key] }));
    });

    protected readonly visibleDays = signal<TrendRange>(DEFAULT_TREND_DAYS);
    protected readonly rangeOptions = computed<Array<FdUiSelectOption<TrendRange>>>(() => {
        this.translationChange();

        return [
            { value: SHORT_TREND_DAYS, label: this.translateService.instant('NUTRITION_TREND.THREE_DAYS') },
            { value: DEFAULT_TREND_DAYS, label: this.translateService.instant('NUTRITION_TREND.SEVEN_DAYS') },
        ];
    });
    protected readonly insightView = computed(() => {
        this.translationChange();
        const keyPrefix = 'NUTRITION_TREND.INSIGHT';
        if (!this.isToday()) {
            return {
                title: this.translateService.instant(`${keyPrefix}.HISTORICAL_TITLE`),
                hint: this.translateService.instant(`${keyPrefix}.HISTORICAL_HINT`),
                comparison: null,
                icon: 'history',
                tone: 'neutral' as const,
                showDetails: false,
            };
        }

        const insight = this.insight();
        const config = INSIGHT_CONFIG[insight.kind];

        return {
            title: this.translateService.instant(`${keyPrefix}.${config.titleKey}`),
            hint: this.translateService.instant(`${keyPrefix}.${config.hintKey}`),
            comparison: this.buildInsightComparison(insight, keyPrefix),
            icon: config.icon,
            tone: insight.tone,
            showDetails: insight.kind !== 'empty',
        };
    });
    protected readonly hasChartData = computed(() => this.visibleSourcePoints().some(point => point.calories > 0));
    private readonly visibleSourcePoints = computed(() => this.points().slice(-this.visibleDays()));
    protected readonly maxCalories = computed(() => {
        const maxStack = Math.max(
            0,
            ...this.visibleSourcePoints().map(point => (this.interactive() ? point.calories : this.calculateStackCalories(point))),
        );
        const upperBound = Math.max(this.dailyGoal(), maxStack);
        return Math.max(CALORIE_SCALE_STEP, Math.ceil(upperBound / CALORIE_SCALE_STEP) * CALORIE_SCALE_STEP);
    });
    protected readonly ticks = computed(() =>
        Array.from({ length: TREND_TICK_COUNT }, (_, index) =>
            Math.round(this.maxCalories() * ((TREND_TICK_COUNT - 1 - index) / (TREND_TICK_COUNT - 1))),
        ),
    );
    protected readonly trendPoints = computed<TrendPoint[]>(() => {
        this.translationChange();
        const locale = resolveTranslateLanguage(this.translateService);
        const points = this.visibleSourcePoints();

        return points.map((point, index) => {
            const proteins = point.proteins ?? 0;
            const fats = point.fats ?? 0;
            const carbs = point.carbs ?? 0;
            const fiber = point.fiber ?? 0;
            return {
                date: point.date,
                dateKey: formatDate(point.date, 'yyyy-MM-dd', locale),
                label: formatDate(point.date, 'd MMM', locale),
                fullDateLabel: formatDate(point.date, 'd MMMM', locale),
                dayLabel: formatDate(point.date, 'd', locale),
                monthLabel: formatDate(point.date, this.interactive() ? 'EEE' : 'MMM', locale),
                calories: point.calories,
                proteins,
                fats,
                carbs,
                fiber,
                isLatest: index === points.length - 1,
            };
        });
    });
    protected readonly barChartCategories = computed<readonly FdUiBarChartCategory[]>(() => {
        this.translationChange();
        return this.trendPoints().map(point => ({
            label: this.interactive() ? `${point.monthLabel}\n${point.dayLabel}` : `${point.dayLabel}\n${point.monthLabel}`,
            ariaLabel: `${point.label}: ${point.calories}`,
            highlighted: this.interactive() ? point.date === this.selectedPoint()?.date : point.isLatest,
            values: [
                ...(this.interactive() && this.calculateStackCalories(point) <= 0
                    ? [
                          {
                              label: this.translateService.instant('GENERAL.CALORIES'),
                              value: Math.max(0, point.calories),
                              color: 'var(--fd-color-text-muted)',
                          },
                      ]
                    : []),
                {
                    label: this.translateService.instant('GENERAL.NUTRIENTS.PROTEIN'),
                    value: this.segmentCalories(point, point.proteins * PROTEIN_CALORIES_PER_GRAM),
                    color: 'var(--fd-color-primary-500)',
                },
                {
                    label: this.translateService.instant('GENERAL.NUTRIENTS.FAT'),
                    value: this.segmentCalories(point, point.fats * FAT_CALORIES_PER_GRAM),
                    color: 'var(--fd-color-orange-500)',
                },
                {
                    label: this.translateService.instant('GENERAL.NUTRIENTS.CARB'),
                    value: this.segmentCalories(point, point.carbs * CARB_CALORIES_PER_GRAM),
                    color: 'var(--fd-color-sky-500)',
                },
                {
                    label: this.translateService.instant('SHARED.NUTRIENTS_SUMMARY.FIBER'),
                    value: this.segmentCalories(point, point.fiber * FIBER_CALORIES_PER_GRAM),
                    color: 'var(--fd-color-rose-500)',
                },
            ],
        }));
    });
    protected readonly barChartReferenceLines = computed<readonly FdUiBarChartReferenceLine[]>(() => {
        this.translationChange();
        if (this.dailyGoal() <= 0) {
            return [];
        }
        const locale = resolveTranslateLanguage(this.translateService);
        return [
            {
                value: this.dailyGoal(),
                label: `${this.translateService.instant('NUTRITION_TREND.GOAL')} ${new Intl.NumberFormat(locale).format(this.dailyGoal())}`,
            },
        ];
    });
    protected readonly formatChartCalories = (value: number): string =>
        new Intl.NumberFormat(resolveTranslateLanguage(this.translateService), { maximumFractionDigits: 0 }).format(value);
    protected changeVisibleDays(value: TrendRange | null | undefined): void {
        this.visibleDays.set(value === SHORT_TREND_DAYS ? SHORT_TREND_DAYS : DEFAULT_TREND_DAYS);
    }

    private segmentCalories(point: NutritionTrendPoint, contribution: number): number {
        const total = this.calculateStackCalories(point);
        return this.interactive() && total > 0 ? (contribution / total) * Math.max(0, point.calories) : contribution;
    }

    private calculateStackCalories(point: NutritionTrendPoint): number {
        return (
            (point.proteins ?? 0) * PROTEIN_CALORIES_PER_GRAM +
            (point.carbs ?? 0) * CARB_CALORIES_PER_GRAM +
            (point.fats ?? 0) * FAT_CALORIES_PER_GRAM +
            (point.fiber ?? 0) * FIBER_CALORIES_PER_GRAM
        );
    }

    private buildInsightComparison(insight: NutritionTrendInsight, keyPrefix: string): string | null {
        if (insight.metric === undefined || insight.current === undefined || insight.goal === undefined) {
            return null;
        }

        const locale = resolveTranslateLanguage(this.translateService);
        const format = (value: number): string => new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(value);
        const params = { current: format(insight.current), goal: format(insight.goal) };
        if (insight.metric === 'calories') {
            return this.translateService.instant(`${keyPrefix}.CALORIE_COMPARISON`, params);
        }

        return this.translateService.instant(`${keyPrefix}.NUTRIENT_COMPARISON`, {
            ...params,
            nutrient: this.translateService.instant(METRIC_LABEL_KEYS[insight.metric]),
        });
    }
}
