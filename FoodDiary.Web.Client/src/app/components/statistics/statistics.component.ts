import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    OnInit,
    computed,
    effect,
    inject,
    signal,
} from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartOptions, ChartTypeRegistry, TooltipItem } from 'chart.js';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { map, finalize, Observable, forkJoin, distinctUntilChanged, startWith } from 'rxjs';

import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { StatisticsService } from '../../services/statistics.service';
import { MappedStatistics, StatisticsMapper } from '../../types/statistics.data';
import { CHART_COLORS } from '../../constants/chart-colors';
import { WeightEntriesService } from '../../services/weight-entries.service';
import { WaistEntriesService } from '../../services/waist-entries.service';
import { WeightEntrySummaryPoint } from '../../types/weight-entry.data';
import { WaistEntrySummaryPoint } from '../../types/waist-entry.data';
import { UserService } from '../../services/user.service';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { PageBodyComponent } from '../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../directives/layout/page-container.directive';
import { PeriodFilterComponent } from '../shared/period-filter/period-filter.component';

type StatisticsRange = 'week' | 'month' | 'year' | 'custom';
type NutritionChartTab = 'calories' | 'macros' | 'distribution';
type BodyChartTab = 'weight' | 'bmi' | 'waist' | 'whtr';

interface DateRange {
    start: Date;
    end: Date;
}

@Component({
    selector: 'fd-statistics',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        ReactiveFormsModule,
        BaseChartDirective,
        FdUiTabsComponent,
        FdUiCardComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        PeriodFilterComponent,
    ],
    templateUrl: './statistics.component.html',
    styleUrls: ['./statistics.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsComponent implements OnInit {
    private readonly statisticsService = inject(StatisticsService);
    private readonly weightEntriesService = inject(WeightEntriesService);
    private readonly waistEntriesService = inject(WaistEntriesService);
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private dateLabelFormatterCache: { locale: string; formatter: Intl.DateTimeFormat } | null = null;
    public readonly currentRange = computed<DateRange>(() => this.getCurrentDateRange());
    public readonly rangeTabs: FdUiTab[] = [
        { value: 'week', labelKey: 'STATISTICS.RANGES.WEEK' },
        { value: 'month', labelKey: 'STATISTICS.RANGES.MONTH' },
        { value: 'year', labelKey: 'STATISTICS.RANGES.YEAR' },
        { value: 'custom', labelKey: 'STATISTICS.RANGES.CUSTOM' },
    ];
    public readonly nutritionTabs: FdUiTab[] = [
        { value: 'calories', labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES' },
        { value: 'macros', labelKey: 'STATISTICS.NUTRITION_TABS.MACROS' },
        { value: 'distribution', labelKey: 'STATISTICS.NUTRITION_TABS.DISTRIBUTION' },
    ];
    public readonly bodyTabs: FdUiTab[] = [
        { value: 'weight', labelKey: 'STATISTICS.BODY_TABS.WEIGHT' },
        { value: 'bmi', labelKey: 'STATISTICS.BODY_TABS.BMI' },
        { value: 'waist', labelKey: 'STATISTICS.BODY_TABS.WAIST' },
        { value: 'whtr', labelKey: 'STATISTICS.BODY_TABS.WHTR' },
    ];

    public readonly selectedRange = signal<StatisticsRange>('month');
    public readonly selectedNutritionTab = signal<NutritionChartTab>('calories');
    public readonly selectedBodyTab = signal<BodyChartTab>('weight');
    public readonly customRangeControl = new FormControl<{ start: Date | null; end: Date | null } | null>(null);

    public readonly isLoading = signal(false);
    public readonly isBodyLoading = signal(false);
    public readonly chartStatisticsData = signal<MappedStatistics | null>(null);
    public readonly weightSummaryPoints = signal<WeightEntrySummaryPoint[]>([]);
    public readonly waistSummaryPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly userHeightCm = signal<number | null>(null);
    private lastLoadedRangeKey: string | null = null;

    public readonly summaryMetrics = computed(() => {
        const stats = this.chartStatisticsData();
        if (!stats) {
            return null;
        }

        const totalCalories = stats.calories.reduce((sum, value) => sum + value, 0);
        const entries = stats.calories.length || 1;
        const averageCalories = totalCalories / entries;
        const aggregated = stats.aggregatedNutrients;

        return {
            totalCalories,
            averageCalories,
            averageCard: {
                consumption: averageCalories,
                steps: 6420,
                burned: 215,
            },
            macros: [
                {
                    labelKey: 'PRODUCT_LIST.PROTEINS',
                    value: aggregated?.proteins ?? 0,
                    color: CHART_COLORS.proteins,
                },
                {
                    labelKey: 'PRODUCT_LIST.FATS',
                    value: aggregated?.fats ?? 0,
                    color: CHART_COLORS.fats,
                },
                {
                    labelKey: 'PRODUCT_LIST.CARBS',
                    value: aggregated?.carbs ?? 0,
                    color: CHART_COLORS.carbs,
                },
                {
                    labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
                    value: aggregated?.fiber ?? 0,
                    color: CHART_COLORS.fiber,
                },
            ],
        };
    });

    public readonly hasStatisticsData = computed(() => (this.chartStatisticsData()?.calories.length ?? 0) > 0);

    public readonly caloriesLineChartData = computed<ChartConfiguration<'line'>['data']>(() => {
        const stats = this.chartStatisticsData();

        return {
            labels: stats?.date.map(date => this.formatDateLabel(date)) ?? [],
            datasets: [
                {
                    data: stats?.calories ?? [],
                    borderColor: CHART_COLORS.primaryLine,
                    backgroundColor: 'transparent',
                    tension: 0.35,
                    pointRadius: 4,
                    pointHoverRadius: 5,
                    borderWidth: 2,
                    fill: false,
                    spanGaps: true,
                    pointBackgroundColor: '#ffffff',
                    pointBorderColor: CHART_COLORS.primaryLine,
                    pointBorderWidth: 2,
                },
            ],
        };
    });

    public readonly nutrientsLineChartData = computed<ChartConfiguration<'line'>['data']>(() => {
        const stats = this.chartStatisticsData();
        const nutrients = stats?.nutrientsStatistic;

        return {
            labels: stats?.date.map(date => this.formatDateLabel(date)) ?? [],
            datasets: [
                {
                    data: nutrients?.proteins ?? [],
                    label: this.translateService.instant('NUTRIENTS.PROTEINS'),
                    borderColor: CHART_COLORS.proteins,
                    backgroundColor: CHART_COLORS.proteins,
                    tension: 0.3,
                    fill: false,
                    spanGaps: true,
                    pointBackgroundColor: '#ffffff',
                    pointBorderColor: CHART_COLORS.proteins,
                    pointBorderWidth: 2,
                    pointRadius: 4,
                },
                {
                    data: nutrients?.fats ?? [],
                    label: this.translateService.instant('NUTRIENTS.FATS'),
                    borderColor: CHART_COLORS.fats,
                    backgroundColor: CHART_COLORS.fats,
                    tension: 0.3,
                    fill: false,
                    spanGaps: true,
                    pointBackgroundColor: '#ffffff',
                    pointBorderColor: CHART_COLORS.fats,
                    pointBorderWidth: 2,
                    pointRadius: 4,
                },
                {
                    data: nutrients?.carbs ?? [],
                    label: this.translateService.instant('NUTRIENTS.CARBS'),
                    borderColor: CHART_COLORS.carbs,
                    backgroundColor: CHART_COLORS.carbs,
                    tension: 0.3,
                    fill: false,
                    spanGaps: true,
                    pointBackgroundColor: '#ffffff',
                    pointBorderColor: CHART_COLORS.carbs,
                    pointBorderWidth: 2,
                    pointRadius: 4,
                },
            ],
        };
    });

    public readonly summarySparklineData = computed<ChartConfiguration<'line'>['data']>(() => {
        const stats = this.chartStatisticsData();
        return {
            labels: stats?.date.map(date => this.formatDateLabel(date)) ?? [],
            datasets: [
                {
                    data: stats?.calories ?? [],
                    borderColor: CHART_COLORS.primaryLine,
                    backgroundColor: 'rgba(37, 99, 235, 0.15)',
                    tension: 0.3,
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 0,
                    spanGaps: true,
                },
            ],
        };
    });

    public readonly nutrientsPieChartData = computed<ChartConfiguration<'pie'>['data']>(() => {
        const aggregated = this.chartStatisticsData()?.aggregatedNutrients;

        return {
            labels: [
                this.translateService.instant('NUTRIENTS.PROTEINS'),
                this.translateService.instant('NUTRIENTS.FATS'),
                this.translateService.instant('NUTRIENTS.CARBS'),
            ],
            datasets: [
                {
                    data: [
                        aggregated?.proteins ?? 0,
                        aggregated?.fats ?? 0,
                        aggregated?.carbs ?? 0,
                    ],
                    backgroundColor: [CHART_COLORS.proteins, CHART_COLORS.fats, CHART_COLORS.carbs],
                    borderWidth: 0,
                },
            ],
        };
    });

    public readonly nutrientsRadarChartData = computed<ChartConfiguration<'radar'>['data']>(() => {
        const aggregated = this.chartStatisticsData()?.aggregatedNutrients;

        return {
            labels: [
                this.translateService.instant('NUTRIENTS.PROTEINS'),
                this.translateService.instant('NUTRIENTS.FATS'),
                this.translateService.instant('NUTRIENTS.CARBS'),
            ],
            datasets: [
                {
                    data: [
                        aggregated?.proteins ?? 0,
                        aggregated?.fats ?? 0,
                        aggregated?.carbs ?? 0,
                    ],
                    backgroundColor: CHART_COLORS.radarBackground,
                    borderColor: CHART_COLORS.radarBorder,
                    borderWidth: 2,
                    pointBackgroundColor: CHART_COLORS.primaryLine,
                },
            ],
        };
    });

    public readonly nutrientsBarChartData = computed<ChartConfiguration<'bar'>['data']>(() => {
        const aggregated = this.chartStatisticsData()?.aggregatedNutrients;

        return {
            labels: [
                this.translateService.instant('NUTRIENTS.PROTEINS'),
                this.translateService.instant('NUTRIENTS.FATS'),
                this.translateService.instant('NUTRIENTS.CARBS'),
                this.translateService.instant('SHARED.NUTRIENTS_SUMMARY.FIBER'),
            ],
            datasets: [
                {
                    data: [
                        aggregated?.proteins ?? 0,
                        aggregated?.fats ?? 0,
                        aggregated?.carbs ?? 0,
                        aggregated?.fiber ?? 0,
                    ],
                    backgroundColor: [
                        CHART_COLORS.proteins,
                        CHART_COLORS.fats,
                        CHART_COLORS.carbs,
                        CHART_COLORS.fiber,
                    ],
                    borderRadius: 6,
                },
            ],
        };
    });

    public readonly bodyChartData = computed<ChartConfiguration<'line'>['data'] | null>(() => {
        const selectedTab = this.selectedBodyTab();
        if (selectedTab === 'weight') {
            return this.createBodyChartDataset(this.weightSummaryPoints(), point => point.averageWeight);
        }

        if (selectedTab === 'waist') {
            return this.createBodyChartDataset(this.waistSummaryPoints(), point => point.averageCircumference);
        }

        if (selectedTab === 'bmi') {
            const heightCm = this.userHeightCm();
            if (!heightCm || heightCm <= 0) {
                return null;
            }
            const heightM = heightCm / 100;
            return this.createBodyChartDataset(this.weightSummaryPoints(), point =>
                point.averageWeight / (heightM * heightM),
            );
        }

        if (selectedTab === 'whtr') {
            const heightCm = this.userHeightCm();
            if (!heightCm || heightCm <= 0) {
                return null;
            }
            return this.createBodyChartDataset(this.waistSummaryPoints(), point =>
                point.averageCircumference / heightCm,
            );
        }

        return null;
    });

    public readonly hasBodyData = computed(
        () =>
            !!this.bodyChartData() &&
            (this.bodyChartData()!.datasets[0].data as (number | null)[]).some(value => value !== null),
    );

    public readonly caloriesLineChartOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
            tooltip: {
                callbacks: {
                    label: context => this.getFormattedTooltip(context, 'PRODUCT_LIST.KCAL'),
                },
            },
        },
        scales: {
            y: {
                beginAtZero: true,
                ticks: {
                    color: '#475569',
                },
            },
            x: {
                ticks: {
                    color: '#475569',
                    maxRotation: 0,
                },
            },
        },
    };

    public readonly nutrientsLineChartOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                position: 'bottom',
            },
        },
        scales: {
            y: {
                beginAtZero: true,
                ticks: { color: '#475569' },
            },
            x: {
                ticks: { color: '#475569', maxRotation: 0 },
            },
        },
    };

    public readonly pieChartOptions: ChartOptions<'pie'> = {
        plugins: {
            tooltip: {
                callbacks: {
                    label: context => this.getFormattedTooltip(context, 'PRODUCT_LIST.GRAMS'),
                },
            },
        },
    };

    public readonly radarChartOptions: ChartOptions<'radar'> = {
        scales: {
            r: {
                beginAtZero: true,
                angleLines: { color: '#cbd5f5' },
                grid: { color: '#e2e8f0' },
                ticks: { showLabelBackdrop: false },
            },
        },
    };

    public readonly barChartOptions: ChartOptions<'bar'> = {
        responsive: true,
        scales: {
            y: {
                beginAtZero: true,
            },
        },
        plugins: {
            legend: { display: false },
        },
    };

    public readonly bodyChartOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
        },
        scales: {
            y: { beginAtZero: false, ticks: { color: '#475569' } },
            x: { ticks: { color: '#475569', maxRotation: 0 } },
        },
    };

    public readonly summarySparklineOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
            tooltip: {
                enabled: false,
            },
        },
        elements: {
            line: { borderJoinStyle: 'round' },
        },
        scales: {
            x: { display: false },
            y: { display: false },
        },
    };

    private readonly customRangeValue = toSignal(
        this.customRangeControl.valueChanges.pipe(
            startWith(this.customRangeControl.value),
            distinctUntilChanged((prev, curr) => {
                const prevStart = prev?.start?.getTime();
                const prevEnd = prev?.end?.getTime();
                const currStart = curr?.start?.getTime();
                const currEnd = curr?.end?.getTime();
                return prevStart === currStart && prevEnd === currEnd;
            }),
        ),
    );

    private readonly rangeEffect = effect(() => {
        const range = this.selectedRange();
        const customRange = this.customRangeValue();

        if (range !== 'custom') {
            this.loadAllData();
            return;
        }

        if (customRange?.start && customRange?.end) {
            this.loadAllData();
        }
    });

    public ngOnInit(): void {
        this.initializeCustomRange();
        this.loadUserProfile();
    }

    public changeRange(value: unknown): void {
        if (!isStatisticsRange(value) || value === this.selectedRange()) {
            return;
        }
        this.selectedRange.set(value);

        const current = this.customRangeControl.value;
        if (!current?.start || !current?.end) {
            const end = new Date();
            const start = new Date(end);
            start.setMonth(start.getMonth() - 1);
            this.customRangeControl.setValue({ start, end }, { emitEvent: true });
        }
    }

    public changeNutritionTab(value: unknown): void {
        if (isNutritionTab(value)) {
            this.selectedNutritionTab.set(value);
        }
    }

    public changeBodyTab(value: unknown): void {
        if (isBodyTab(value)) {
            this.selectedBodyTab.set(value);
        }
    }

    private loadAllData(): void {
        const range = this.getCurrentDateRange();
        const normalizedStart = this.normalizeStartOfDay(range.start);
        const normalizedEnd = this.normalizeEndOfDay(range.end);
        const rangeKey = `${normalizedStart.toISOString()}_${normalizedEnd.toISOString()}`;

        if (rangeKey === this.lastLoadedRangeKey) {
            return;
        }

        this.lastLoadedRangeKey = rangeKey;

        this.loadStatistics(range);
        this.loadBodySummaries(range);
    }

    private loadStatistics(range: DateRange): void {
        this.isLoading.set(true);
        const normalizedStart = this.normalizeStartOfDay(range.start);
        const normalizedEnd = this.normalizeEndOfDay(range.end);
        const quantizationDays = this.getQuantizationDays(normalizedStart, normalizedEnd);

        this.statisticsService
            .getAggregatedStatistics({
                dateFrom: normalizedStart,
                dateTo: normalizedEnd,
                quantizationDays,
            })
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: data => {
                    const mappedData = StatisticsMapper.mapStatistics(data ?? []);
                    this.chartStatisticsData.set(mappedData);
                },
            });
    }

    private loadBodySummaries(range: DateRange): void {
        this.isBodyLoading.set(true);
        const normalizedStart = this.normalizeStartOfDay(range.start).toISOString();
        const normalizedEnd = this.normalizeEndOfDay(range.end).toISOString();
        const quantizationDays = this.getQuantizationDays(range.start, range.end);

        forkJoin({
            weight: this.weightEntriesService.getSummary({
                dateFrom: normalizedStart,
                dateTo: normalizedEnd,
                quantizationDays,
            }),
            waist: this.waistEntriesService.getSummary({
                dateFrom: normalizedStart,
                dateTo: normalizedEnd,
                quantizationDays,
            }),
        })
            .pipe(
                finalize(() => this.isBodyLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: ({ weight, waist }) => {
                    this.weightSummaryPoints.set(weight);
                    this.waistSummaryPoints.set(waist);
                },
                error: () => {
                    this.weightSummaryPoints.set([]);
                    this.waistSummaryPoints.set([]);
                },
            });
    }

    private loadUserProfile(): void {
        this.userService
            .getInfo()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(user => {
                this.userHeightCm.set(user?.height ?? null);
            });
    }

    private initializeCustomRange(): void {
        const end = new Date();
        const start = new Date(end);
        start.setMonth(start.getMonth() - 1);
        this.customRangeControl.setValue({ start, end }, { emitEvent: false });
    }

    private getCurrentDateRange(): DateRange {
        const range = this.selectedRange();
        const end = new Date();
        const start = new Date(end);

        if (range === 'week') {
            start.setDate(end.getDate() - 7);
            return { start, end };
        }

        if (range === 'month') {
            start.setMonth(end.getMonth() - 1);
            return { start, end };
        }

        if (range === 'year') {
            start.setFullYear(end.getFullYear() - 1);
            return { start, end };
        }

        const custom = this.customRangeControl.value;
        if (custom?.start && custom?.end) {
            return { start: custom.start, end: custom.end };
        }

        return { start, end };
    }

    private getQuantizationDays(start: Date, end: Date): number {
        const totalDays = Math.max(1, Math.round((end.getTime() - start.getTime()) / MS_IN_DAY));

        if (totalDays > 180) {
            return 30;
        }

        if (totalDays > 120) {
            return 21;
        }

        if (totalDays > 90) {
            return 14;
        }

        if (totalDays > 60) {
            return 7;
        }

        if (totalDays > 30) {
            return 3;
        }

        if (totalDays > 14) {
            return 2;
        }

        return 1;
    }

    private formatDateLabel(date: Date): string {
        return this.getDateLabelFormatter().format(date);
    }

    private getDateLabelFormatter(): Intl.DateTimeFormat {
        const locale = this.getCurrentLocale();
        if (!this.dateLabelFormatterCache || this.dateLabelFormatterCache.locale !== locale) {
            this.dateLabelFormatterCache = {
                locale,
                formatter: new Intl.DateTimeFormat(locale, { month: 'short', day: 'numeric' }),
            };
        }

        return this.dateLabelFormatterCache.formatter;
    }

    private getCurrentLocale(): string {
        return this.translateService.currentLang || this.translateService.defaultLang || 'en-US';
    }

    private createBodyChartDataset<T extends { dateFrom: string }>(
        points: T[],
        getValue: (point: T) => number | null | undefined,
    ): ChartConfiguration<'line'>['data'] | null {
        if (!points.length) {
            return null;
        }

        const labels: string[] = [];
        const data: (number | null)[] = [];

        points.forEach(point => {
            labels.push(this.formatSummaryLabel(point.dateFrom));
            const value = getValue(point);
            if (value === undefined || value === null || Number.isNaN(value)) {
                data.push(null);
            } else {
                data.push(Number(value.toFixed(2)));
            }
        });

        if (data.every(value => value === null)) {
            return null;
        }

        return {
            labels,
            datasets: [
                {
                    data,
                    borderColor: CHART_COLORS.primaryLine,
                    backgroundColor: 'transparent',
                    tension: 0.3,
                    pointRadius: 4,
                    borderWidth: 2,
                    spanGaps: true,
                    fill: false,
                    pointBackgroundColor: '#ffffff',
                    pointBorderColor: CHART_COLORS.primaryLine,
                    pointBorderWidth: 2,
                },
            ],
        };
    }

    private formatSummaryLabel(dateString: string): string {
        const date = new Date(dateString);
        return date.toLocaleDateString(this.getCurrentLocale());
    }

    private normalizeStartOfDay(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    }

    private normalizeEndOfDay(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999));
    }

    private getFormattedTooltip<T extends keyof ChartTypeRegistry>(
        context: TooltipItem<T>,
        key: string,
    ): string | string[] {
        const label = context.label || '';
        const value = Number(context.raw) || 0;
        const formattedValue = parseFloat(value.toFixed(2));

        return `${label}: ${formattedValue} ${this.translateService.instant(key)}`;
    }
}

const MS_IN_DAY = 24 * 60 * 60 * 1000;

function isStatisticsRange(value: unknown): value is StatisticsRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}

function isNutritionTab(value: unknown): value is NutritionChartTab {
    return value === 'calories' || value === 'macros' || value === 'distribution';
}

function isBodyTab(value: unknown): value is BodyChartTab {
    return value === 'weight' || value === 'bmi' || value === 'waist' || value === 'whtr';
}
