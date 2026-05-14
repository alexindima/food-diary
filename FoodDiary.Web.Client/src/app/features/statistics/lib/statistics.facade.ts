import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import { distinctUntilChanged, finalize, forkJoin, startWith } from 'rxjs';

import { UserService } from '../../../shared/api/user.service';
import { CENTIMETERS_PER_METER } from '../../../shared/lib/body-measurement.constants';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { type ExportFormat, ExportService } from '../../meals/api/export.service';
import { WaistEntriesService } from '../../waist-history/api/waist-entries.service';
import type { WaistEntrySummaryPoint } from '../../waist-history/models/waist-entry.data';
import { WeightEntriesService } from '../../weight-history/api/weight-entries.service';
import type { WeightEntrySummaryPoint } from '../../weight-history/models/weight-entry.data';
import { StatisticsService } from '../api/statistics.service';
import type { MappedStatistics } from '../models/statistics.data';
import {
    type BodyChartTab,
    buildBodyChartData,
    buildCaloriesLineChartData,
    buildMacroSparklineData,
    buildNutrientsBarChartData,
    buildNutrientsLineChartData,
    buildNutrientsPieChartData,
    buildNutrientsRadarChartData,
    buildSummaryMetrics,
    buildSummarySparklineData,
    type DateRange,
    getCurrentDateRange,
    getQuantizationDays,
    normalizeEndOfDay,
    normalizeStartOfDay,
    type NutritionChartTab,
    type StatisticsRange,
} from './statistics-data-mapper';
import { buildStatisticsExportRequest } from './statistics-export.mapper';
import { mapStatistics } from './statistics-statistics.mapper';

@Injectable({ providedIn: 'root' })
export class StatisticsFacade {
    private readonly statisticsService = inject(StatisticsService);
    private readonly weightEntriesService = inject(WeightEntriesService);
    private readonly waistEntriesService = inject(WaistEntriesService);
    private readonly userService = inject(UserService);
    private readonly exportService = inject(ExportService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    private dateLabelFormatterCache: { locale: string; formatter: Intl.DateTimeFormat } | null = null;
    private lastLoadedRangeKey: string | null = null;
    private readonly initialized = signal(false);

    public readonly selectedRange = signal<StatisticsRange>('week');
    public readonly selectedNutritionTab = signal<NutritionChartTab>('calories');
    public readonly selectedBodyTab = signal<BodyChartTab>('weight');
    public readonly customRangeControl = new FormControl<{ start: Date | null; end: Date | null } | null>(null);

    public readonly isLoading = signal(false);
    public readonly isBodyLoading = signal(false);
    public readonly hasLoadError = signal(false);
    public readonly hasBodyLoadError = signal(false);
    public readonly exportingFormat = signal<ExportFormat | null>(null);
    public readonly chartStatisticsData = signal<MappedStatistics | null>(null);
    public readonly weightSummaryPoints = signal<WeightEntrySummaryPoint[]>([]);
    public readonly waistSummaryPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly userHeightCm = signal<number | null>(null);

    public readonly currentRange = computed<DateRange>(() => getCurrentDateRange(this.selectedRange(), this.customRangeControl.value));
    public readonly summaryMetrics = computed(() => buildSummaryMetrics(this.chartStatisticsData()));
    public readonly macroSparklineData = computed(() =>
        buildMacroSparklineData(this.chartStatisticsData(), date => this.formatDateLabel(date)),
    );
    public readonly hasStatisticsData = computed(() => (this.chartStatisticsData()?.calories.length ?? 0) > 0);
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
            if (heightCm === null || heightCm <= 0) {
                return null;
            }

            const heightM = heightCm / CENTIMETERS_PER_METER;
            return buildBodyChartData(this.weightSummaryPoints(), point => point.averageWeight / (heightM * heightM), formatLabel);
        }

        const heightCm = this.userHeightCm();
        if (heightCm === null || heightCm <= 0) {
            return null;
        }

        return buildBodyChartData(this.waistSummaryPoints(), point => point.averageCircumference / heightCm, formatLabel);
    });
    public readonly hasBodyData = computed(() => {
        const bodyChartData = this.bodyChartData();
        const values = bodyChartData?.datasets[0].data;
        return Array.isArray(values) && values.some(value => value !== null);
    });

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

    public constructor() {
        effect(() => {
            if (!this.initialized()) {
                return;
            }

            const range = this.selectedRange();
            const customRange = this.customRangeValue();

            if (range !== 'custom') {
                this.loadAllData();
                return;
            }

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

        const current = this.customRangeControl.value;
        if (current?.start === undefined || current.start === null || current.end === null) {
            const end = new Date();
            const start = new Date(end);
            start.setMonth(start.getMonth() - 1);
            this.customRangeControl.setValue({ start, end }, { emitEvent: true });
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
                    currentLang: this.translateService.getCurrentLang(),
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
        this.hasLoadError.set(false);
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
                finalize(() => {
                    this.isLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: data => {
                    this.chartStatisticsData.set(mapStatistics(data));
                    this.hasLoadError.set(false);
                },
                error: () => {
                    this.chartStatisticsData.set(null);
                    this.hasLoadError.set(true);
                },
            });
    }

    private loadBodySummaries(range: DateRange): void {
        this.isBodyLoading.set(true);
        this.hasBodyLoadError.set(false);
        const normalizedStart = normalizeStartOfDay(range.start).toISOString();
        const normalizedEnd = normalizeEndOfDay(range.end).toISOString();
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
            .pipe(
                finalize(() => {
                    this.isBodyLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: ({ weight, waist }) => {
                    this.weightSummaryPoints.set(weight);
                    this.waistSummaryPoints.set(waist);
                    this.hasBodyLoadError.set(false);
                },
                error: () => {
                    this.weightSummaryPoints.set([]);
                    this.waistSummaryPoints.set([]);
                    this.hasBodyLoadError.set(true);
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

    private formatDateLabel(date: Date): string {
        return this.getDateLabelFormatter().format(date);
    }

    private getDateLabelFormatter(): Intl.DateTimeFormat {
        const locale = this.getCurrentLocale();
        if (this.dateLabelFormatterCache?.locale !== locale) {
            this.dateLabelFormatterCache = {
                locale,
                formatter: new Intl.DateTimeFormat(locale, { month: 'short', day: 'numeric' }),
            };
        }

        return this.dateLabelFormatterCache.formatter;
    }

    private getCurrentLocale(): string {
        const currentLang = this.translateService.getCurrentLang();
        if (currentLang.length > 0) {
            return resolveAppLocale(currentLang);
        }

        const fallbackLang = this.translateService.getFallbackLang();
        return resolveAppLocale(fallbackLang);
    }

    private formatSummaryLabel(dateString: string): string {
        return this.formatDateLabel(new Date(dateString));
    }
}
