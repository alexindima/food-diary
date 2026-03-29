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
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ChartConfiguration } from 'chart.js';
import { finalize, forkJoin, distinctUntilChanged, startWith } from 'rxjs';

import { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { PeriodFilterComponent } from '../../../components/shared/period-filter/period-filter.component';
import { StatisticsBodyComponent } from '../../../components/shared/statistics-body/statistics-body.component';
import { StatisticsNutritionComponent } from '../../../components/shared/statistics-nutrition/statistics-nutrition.component';
import { StatisticsSummaryComponent } from '../../../components/shared/statistics-summary/statistics-summary.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { UserService } from '../../../shared/api/user.service';
import { WaistEntriesService } from '../../waist-history/api/waist-entries.service';
import { WeightEntriesService } from '../../weight-history/api/weight-entries.service';
import { WaistEntrySummaryPoint } from '../../waist-history/models/waist-entry.data';
import { WeightEntrySummaryPoint } from '../../weight-history/models/weight-entry.data';
import { StatisticsService } from '../api/statistics.service';
import {
    createCaloriesLineChartOptions,
    createPieChartOptions,
    nutrientsLineChartOptions,
    radarChartOptions,
    barChartOptions,
    bodyChartOptions,
    summarySparklineOptions,
} from '../lib/statistics-chart-config';
import {
    type StatisticsRange,
    type NutritionChartTab,
    type BodyChartTab,
    type DateRange,
    isStatisticsRange,
    isNutritionTab,
    isBodyTab,
    getQuantizationDays,
    normalizeStartOfDay,
    normalizeEndOfDay,
    getCurrentDateRange,
    buildCaloriesLineChartData,
    buildNutrientsLineChartData,
    buildNutrientsPieChartData,
    buildNutrientsRadarChartData,
    buildNutrientsBarChartData,
    buildBodyChartData,
    buildSummaryMetrics,
    buildMacroSparklineData,
    buildSummarySparklineData,
} from '../lib/statistics-data-mapper';
import { MappedStatistics, StatisticsMapper } from '../models/statistics.data';

@Component({
    selector: 'fd-statistics',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        ReactiveFormsModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        PeriodFilterComponent,
        StatisticsSummaryComponent,
        StatisticsNutritionComponent,
        StatisticsBodyComponent,
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
    private lastLoadedRangeKey: string | null = null;

    // ── Tab definitions ────────────────────────────────────────────────

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

    // ── State signals ──────────────────────────────────────────────────

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

    // ── Chart options (static or tooltip-bound) ────────────────────────

    public readonly caloriesLineChartOptions = createCaloriesLineChartOptions(
        (label, value) => `${label}: ${parseFloat(value.toFixed(2))} ${this.translateService.instant('GENERAL.UNITS.KCAL')}`,
    );
    public readonly nutrientsLineChartOptions = nutrientsLineChartOptions;
    public readonly pieChartOptions = createPieChartOptions(
        (label, value) => `${label}: ${parseFloat(value.toFixed(2))} ${this.translateService.instant('GENERAL.UNITS.G')}`,
    );
    public readonly radarChartOptions = radarChartOptions;
    public readonly barChartOptions = barChartOptions;
    public readonly bodyChartOptions = bodyChartOptions;
    public readonly summarySparklineOptions = summarySparklineOptions;

    // ── Computed signals ───────────────────────────────────────────────

    public readonly currentRange = computed<DateRange>(() =>
        getCurrentDateRange(this.selectedRange(), this.customRangeControl.value),
    );

    public readonly summaryMetrics = computed(() =>
        buildSummaryMetrics(this.chartStatisticsData()),
    );

    public readonly macroSparklineData = computed(() =>
        buildMacroSparklineData(this.chartStatisticsData(), date => this.formatDateLabel(date)),
    );

    public readonly hasStatisticsData = computed(() =>
        (this.chartStatisticsData()?.calories.length ?? 0) > 0,
    );

    public readonly caloriesLineChartData = computed(() =>
        buildCaloriesLineChartData(this.chartStatisticsData(), date => this.formatDateLabel(date)),
    );

    public readonly nutrientsLineChartData = computed(() =>
        buildNutrientsLineChartData(
            this.chartStatisticsData(),
            date => this.formatDateLabel(date),
            key => this.translateService.instant(key),
        ),
    );

    public readonly summarySparklineData = computed(() =>
        buildSummarySparklineData(this.chartStatisticsData(), date => this.formatDateLabel(date)),
    );

    public readonly nutrientsPieChartData = computed(() =>
        buildNutrientsPieChartData(this.chartStatisticsData(), key => this.translateService.instant(key)),
    );

    public readonly nutrientsRadarChartData = computed(() =>
        buildNutrientsRadarChartData(this.chartStatisticsData(), key => this.translateService.instant(key)),
    );

    public readonly nutrientsBarChartData = computed(() =>
        buildNutrientsBarChartData(this.chartStatisticsData(), key => this.translateService.instant(key)),
    );

    public readonly bodyChartData = computed<ChartConfiguration<'line'>['data'] | null>(() => {
        const selectedTab = this.selectedBodyTab();
        const formatLabel = (dateString: string): string => this.formatSummaryLabel(dateString);

        if (selectedTab === 'weight') {
            return buildBodyChartData(this.weightSummaryPoints(), point => point.averageWeight, formatLabel);
        }

        if (selectedTab === 'waist') {
            return buildBodyChartData(this.waistSummaryPoints(), point => point.averageCircumference, formatLabel);
        }

        if (selectedTab === 'bmi') {
            const heightCm = this.userHeightCm();
            if (!heightCm || heightCm <= 0) {
                return null;
            }

            const heightM = heightCm / 100;
            return buildBodyChartData(
                this.weightSummaryPoints(),
                point => point.averageWeight / (heightM * heightM),
                formatLabel,
            );
        }

        if (selectedTab === 'whtr') {
            const heightCm = this.userHeightCm();
            if (!heightCm || heightCm <= 0) {
                return null;
            }

            return buildBodyChartData(
                this.waistSummaryPoints(),
                point => point.averageCircumference / heightCm,
                formatLabel,
            );
        }

        return null;
    });

    public readonly hasBodyData = computed(
        () =>
            !!this.bodyChartData() &&
            (this.bodyChartData()!.datasets[0].data as (number | null)[]).some(value => value !== null),
    );

    // ── Reactive plumbing ──────────────────────────────────────────────

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

    // ── Lifecycle ──────────────────────────────────────────────────────

    public ngOnInit(): void {
        this.initializeCustomRange();
        this.loadUserProfile();
    }

    // ── Public handlers ────────────────────────────────────────────────

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

    // ── Private data loading ───────────────────────────────────────────

    private loadAllData(): void {
        const range = getCurrentDateRange(this.selectedRange(), this.customRangeControl.value);
        const normalizedStart = normalizeStartOfDay(range.start);
        const normalizedEnd = normalizeEndOfDay(range.end);
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
        const normalizedStart = normalizeStartOfDay(range.start);
        const normalizedEnd = normalizeEndOfDay(range.end);
        const quantizationDays = getQuantizationDays(normalizedStart, normalizedEnd);

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
        const normalizedStart = normalizeStartOfDay(range.start).toISOString();
        const normalizedEnd = normalizeEndOfDay(range.end).toISOString();
        const quantizationDays = getQuantizationDays(range.start, range.end);

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

    // ── Private helpers ────────────────────────────────────────────────

    private initializeCustomRange(): void {
        const end = new Date();
        const start = new Date(end);
        start.setMonth(start.getMonth() - 1);
        this.customRangeControl.setValue({ start, end }, { emitEvent: false });
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

    private formatSummaryLabel(dateString: string): string {
        const date = new Date(dateString);
        return date.toLocaleDateString(this.getCurrentLocale());
    }
}
