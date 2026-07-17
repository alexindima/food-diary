import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { finalize, forkJoin } from 'rxjs';

import { ExportService } from '../../../shared/api/export.service';
import { UserService } from '../../../shared/api/user.service';
import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { CENTIMETERS_PER_METER } from '../../../shared/lib/body-measurement.constants';
import { formatDateInputValue, parseLocalDateInputValue } from '../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { RequestStateController } from '../../../shared/lib/request-state';
import type { ExportFormat } from '../../../shared/models/export.models';
import { WaistEntriesService } from '../../waist-history/api/waist-entries.service';
import type { WaistEntrySummaryPoint } from '../../waist-history/models/waist-entry.data';
import { WeightEntriesService } from '../../weight-history/api/weight-entries.service';
import type { WeightEntrySummaryPoint } from '../../weight-history/models/weight-entry.data';
import { StatisticsService } from '../api/statistics.service';
import type { MappedStatistics } from '../models/statistics.data';
import {
    type BodyChartTab,
    buildBodyChartPoints,
    buildCaloriesTrendPoints,
    buildMacroSparklinePoints,
    buildNutrientBarItems,
    buildNutrientPieSegments,
    buildNutrientTrendGroups,
    buildSummaryMetrics,
    buildSummarySparklinePoints,
    type DateRange,
    getCurrentDateRange,
    getDateRangeDayCount,
    getQuantizationDays,
    normalizeEndOfDay,
    normalizeStartOfDay,
    type NutritionChartTab,
    type StatisticsRange,
} from './statistics-data-mapper';
import { buildStatisticsExportRequest } from './statistics-export.mapper';
import { mapStatistics } from './statistics-statistics.mapper';

@Injectable()
export class StatisticsFacade {
    private readonly statisticsService = inject(StatisticsService);
    private readonly weightEntriesService = inject(WeightEntriesService);
    private readonly waistEntriesService = inject(WaistEntriesService);
    private readonly userService = inject(UserService);
    private readonly exportService = inject(ExportService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    private dateLabelFormatterCache: { locale: string; range: StatisticsRange; formatter: Intl.DateTimeFormat } | null = null;
    private lastLoadedRangeKey: string | null = null;
    private readonly statisticsRequest = new RequestStateController<MappedStatistics, 'STATISTICS.LOAD_ERROR'>();
    private readonly bodyRequest = new RequestStateController<
        { weight: WeightEntrySummaryPoint[]; waist: WaistEntrySummaryPoint[] },
        'STATISTICS.BODY_LOAD_ERROR'
    >();
    private readonly initialized = signal(false);
    private readonly currentLocale = signal(this.resolveCurrentLocale());

    public readonly selectedRange = signal<StatisticsRange>('week');
    public readonly selectedNutritionTab = signal<NutritionChartTab>('calories');
    public readonly selectedBodyTab = signal<BodyChartTab>('weight');
    public readonly customRangeModel = signal<{ range: { start: Date | null; end: Date | null } | null }>({ range: null });
    public readonly customRangeForm = form(this.customRangeModel);

    public readonly isLoading = this.statisticsRequest.isLoading;
    public readonly isBodyLoading = this.bodyRequest.isLoading;
    public readonly hasLoadError = computed(() => this.statisticsRequest.error() !== null);
    public readonly hasBodyLoadError = computed(() => this.bodyRequest.error() !== null);
    public readonly exportingFormat = signal<ExportFormat | null>(null);
    public readonly chartStatisticsData = this.statisticsRequest.data;
    public readonly weightSummaryPoints = computed(() => this.bodyRequest.data()?.weight ?? []);
    public readonly waistSummaryPoints = computed(() => this.bodyRequest.data()?.waist ?? []);
    public readonly userHeightCm = signal<number | null>(null);

    public readonly currentRange = computed<DateRange>(() => {
        const selectedRange = this.selectedRange();

        return getCurrentDateRange(selectedRange, selectedRange === 'custom' ? this.customRangeModel().range : null);
    });
    public readonly summaryMetrics = computed(() =>
        buildSummaryMetrics(this.chartStatisticsData(), getDateRangeDayCount(this.currentRange())),
    );
    public readonly macroSparklinePoints = computed(() =>
        buildMacroSparklinePoints(this.chartStatisticsData(), date => this.formatDateLabel(date)),
    );
    public readonly hasStatisticsData = computed(() => (this.chartStatisticsData()?.calories.length ?? 0) > 0);
    public readonly caloriesTrendPoints = computed(() =>
        buildCaloriesTrendPoints(this.chartStatisticsData(), date => this.formatDateLabel(date)),
    );
    public readonly nutrientTrendGroups = computed(() =>
        buildNutrientTrendGroups(
            this.chartStatisticsData(),
            date => this.formatDateLabel(date),
            key => this.translateKey(key),
        ),
    );
    public readonly summarySparklinePoints = computed(() =>
        buildSummarySparklinePoints(this.chartStatisticsData(), date => this.formatDateLabel(date)),
    );
    public readonly nutrientPieSegments = computed(() =>
        buildNutrientPieSegments(this.chartStatisticsData(), key => this.translateKey(key)),
    );
    public readonly nutrientBarItems = computed(() => buildNutrientBarItems(this.chartStatisticsData(), key => this.translateKey(key)));
    public readonly bodyChartPoints = computed(() => {
        const selectedTab = this.selectedBodyTab();
        const formatLabel = (dateString: string): string => this.formatSummaryLabel(dateString);

        if (selectedTab === 'weight') {
            return buildBodyChartPoints(this.weightSummaryPoints(), point => point.averageWeight, formatLabel);
        }

        if (selectedTab === 'waist') {
            return buildBodyChartPoints(this.waistSummaryPoints(), point => point.averageCircumference, formatLabel);
        }

        if (selectedTab === 'bmi') {
            const heightCm = this.userHeightCm();
            if (heightCm === null || heightCm <= 0) {
                return [];
            }

            const heightM = heightCm / CENTIMETERS_PER_METER;
            return buildBodyChartPoints(this.weightSummaryPoints(), point => point.averageWeight / (heightM * heightM), formatLabel);
        }

        const heightCm = this.userHeightCm();
        if (heightCm === null || heightCm <= 0) {
            return [];
        }

        return buildBodyChartPoints(this.waistSummaryPoints(), point => point.averageCircumference / heightCm, formatLabel);
    });
    public readonly hasBodyData = computed(() => {
        return this.bodyChartPoints().some(point => point.value !== null);
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.dateLabelFormatterCache = null;
            this.currentLocale.set(this.resolveCurrentLocale());
        });

        effect(() => {
            if (!this.initialized()) {
                return;
            }

            const range = this.selectedRange();

            if (range !== 'custom') {
                this.loadAllData();
                return;
            }

            const customRange = this.customRangeModel().range;
            if (customRange?.start !== null && customRange?.end !== null) {
                this.loadAllData();
            }
        });
    }

    public initialize(): void {
        if (this.initialized()) {
            return;
        }

        this.initialized.set(true);
        this.initializeCustomRange();
        this.loadAllData();
        this.loadUserProfile();
    }

    public changeRange(value: StatisticsRange): void {
        if (value === this.selectedRange()) {
            return;
        }

        this.selectedRange.set(value);

        const current = this.customRangeModel().range;
        if (current?.start === undefined || current.start === null || current.end === null) {
            const end = new Date();
            const start = new Date(end);
            start.setMonth(start.getMonth() - 1);
            this.customRangeModel.set({ range: { start, end } });
        }
    }

    public changeNutritionTab(value: NutritionChartTab): void {
        this.selectedNutritionTab.set(value);
    }

    public changeBodyTab(value: BodyChartTab): void {
        this.selectedBodyTab.set(value);
    }

    public reload(): void {
        this.lastLoadedRangeKey = null;
        this.loadAllData();
    }

    public exportDiary(format: ExportFormat): void {
        if (this.exportingFormat() !== null) {
            return;
        }

        this.exportingFormat.set(format);
        this.exportService
            .exportDiary(
                buildStatisticsExportRequest({
                    range: this.currentRange(),
                    format,
                    currentLang: resolveTranslateLanguage(this.translateService),
                    fallbackLang: this.translateService.getFallbackLang(),
                    timeZoneOffsetMinutes: -new Date().getTimezoneOffset(),
                }),
            )
            .pipe(
                finalize(() => {
                    this.exportingFormat.set(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    private loadAllData(): void {
        const range = getCurrentDateRange(this.selectedRange(), this.customRangeModel().range);
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
        const requestId = this.statisticsRequest.begin();
        const normalizedStart = normalizeStartOfDay(range.start);
        const normalizedEnd = normalizeEndOfDay(range.end);
        const quantizationDays = getQuantizationDays(normalizedStart, normalizedEnd);

        this.statisticsService
            .getAggregatedStatistics({
                dateFrom: normalizedStart,
                dateTo: normalizedEnd,
                quantizationDays,
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: data => {
                    this.statisticsRequest.succeed(requestId, mapStatistics(data));
                },
                error: () => {
                    this.statisticsRequest.fail(requestId, 'STATISTICS.LOAD_ERROR', { preserveData: false });
                },
            });
    }

    private loadBodySummaries(range: DateRange): void {
        const requestId = this.bodyRequest.begin();
        const normalizedStart = formatDateInputValue(range.start);
        const normalizedEnd = formatDateInputValue(range.end);
        const quantizationDays = getQuantizationDays(normalizeStartOfDay(range.start), normalizeEndOfDay(range.end));

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
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: ({ weight, waist }) => {
                    this.bodyRequest.succeed(requestId, { weight, waist });
                },
                error: () => {
                    this.bodyRequest.fail(requestId, 'STATISTICS.BODY_LOAD_ERROR', { preserveData: false });
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
        this.customRangeModel.set({ range: { start, end } });
    }

    private formatDateLabel(date: Date): string {
        const label = this.getDateLabelFormatter().format(date);

        return this.selectedRange() === 'year' ? this.capitalizeFirstLetter(label) : label;
    }

    private getDateLabelFormatter(): Intl.DateTimeFormat {
        const locale = this.currentLocale();
        const range = this.selectedRange();
        if (this.dateLabelFormatterCache?.locale !== locale || this.dateLabelFormatterCache.range !== range) {
            this.dateLabelFormatterCache = {
                locale,
                range,
                formatter: new Intl.DateTimeFormat(locale, range === 'year' ? { month: 'short' } : { month: 'short', day: 'numeric' }),
            };
        }

        return this.dateLabelFormatterCache.formatter;
    }

    private resolveCurrentLocale(): string {
        return resolveAppLocale(resolveTranslateLanguage(this.translateService));
    }

    private translateKey(key: string): string {
        this.currentLocale();
        return this.translateService.instant(key);
    }

    private capitalizeFirstLetter(value: string): string {
        if (value.length === 0) {
            return value;
        }

        return `${value.at(0)?.toLocaleUpperCase(this.currentLocale()) ?? ''}${value.slice(1)}`;
    }

    private formatSummaryLabel(dateString: string): string {
        const date = parseLocalDateInputValue(dateString) ?? new Date(dateString);

        return this.formatDateLabel(date);
    }
}
