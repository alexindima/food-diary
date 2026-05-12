import type { ChartConfiguration } from 'chart.js';

import type { SummaryMetrics } from '../../../components/shared/statistics-summary/statistics-summary.component';
import { CHART_COLORS } from '../../../constants/chart-colors';
import { normalizeEndOfLocalDay, normalizeStartOfLocalDay } from '../../../shared/lib/local-date.utils';
import type { MappedStatistics } from '../models/statistics.data';
import { applyAlpha } from './statistics-chart-config';

// ── Types ──────────────────────────────────────────────────────────────

export type StatisticsRange = 'week' | 'month' | 'year' | 'custom';
export type NutritionChartTab = 'calories' | 'macros' | 'distribution';
export type BodyChartTab = 'weight' | 'bmi' | 'waist' | 'whtr';
export type MacroKey = 'proteins' | 'fats' | 'carbs' | 'fiber';

export interface DateRange {
    start: Date;
    end: Date;
}

// ── Validation guards ──────────────────────────────────────────────────

export function isStatisticsRange(value: unknown): value is StatisticsRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}

export function isNutritionTab(value: unknown): value is NutritionChartTab {
    return value === 'calories' || value === 'macros' || value === 'distribution';
}

export function isBodyTab(value: unknown): value is BodyChartTab {
    return value === 'weight' || value === 'bmi' || value === 'waist' || value === 'whtr';
}

// ── Date range helpers ─────────────────────────────────────────────────

const HOURS_PER_DAY = 24;
const MINUTES_PER_HOUR = 60;
const SECONDS_PER_MINUTE = 60;
const MS_PER_SECOND = 1_000;
const MS_IN_DAY = HOURS_PER_DAY * MINUTES_PER_HOUR * SECONDS_PER_MINUTE * MS_PER_SECOND;
const HALF_YEAR_DAYS = 180;
const LONG_RANGE_QUANTIZATION_DAYS = 30;
const FOUR_MONTH_DAYS = 120;
const FOUR_MONTH_QUANTIZATION_DAYS = 21;
const QUARTER_DAYS = 90;
const QUARTER_QUANTIZATION_DAYS = 14;
const TWO_MONTH_DAYS = 60;
const TWO_MONTH_QUANTIZATION_DAYS = 7;
const MONTH_DAYS = 30;
const MONTH_QUANTIZATION_DAYS = 3;
const TWO_WEEK_DAYS = 14;
const TWO_WEEK_QUANTIZATION_DAYS = 2;
const WEEK_DAY_OFFSET = 6;
const DEFAULT_FILL_ALPHA = 0.16;
const MACRO_SPARKLINE_FILL_ALPHA = 0.18;

export function getQuantizationDays(start: Date, end: Date): number {
    const totalDays = Math.max(1, Math.round((end.getTime() - start.getTime()) / MS_IN_DAY));

    if (totalDays > HALF_YEAR_DAYS) {
        return LONG_RANGE_QUANTIZATION_DAYS;
    }

    if (totalDays > FOUR_MONTH_DAYS) {
        return FOUR_MONTH_QUANTIZATION_DAYS;
    }

    if (totalDays > QUARTER_DAYS) {
        return QUARTER_QUANTIZATION_DAYS;
    }

    if (totalDays > TWO_MONTH_DAYS) {
        return TWO_MONTH_QUANTIZATION_DAYS;
    }

    if (totalDays > MONTH_DAYS) {
        return MONTH_QUANTIZATION_DAYS;
    }

    if (totalDays > TWO_WEEK_DAYS) {
        return TWO_WEEK_QUANTIZATION_DAYS;
    }

    return 1;
}

export function normalizeStartOfDay(date: Date): Date {
    return normalizeStartOfLocalDay(date);
}

export function normalizeEndOfDay(date: Date): Date {
    return normalizeEndOfLocalDay(date);
}

export function getCurrentDateRange(
    range: StatisticsRange,
    customValue: { start: Date | null; end: Date | null } | null | undefined,
): DateRange {
    const end = new Date();
    const start = new Date(end);

    if (range === 'week') {
        start.setDate(end.getDate() - WEEK_DAY_OFFSET);
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

    const customStart = customValue?.start ?? null;
    const customEnd = customValue?.end ?? null;
    if (customStart !== null && customEnd !== null) {
        return { start: customStart, end: customEnd };
    }

    return { start, end };
}

// ── Chart data builders ────────────────────────────────────────────────

export function buildCaloriesLineChartData(
    stats: MappedStatistics | null,
    formatLabel: (date: Date) => string,
): ChartConfiguration<'line'>['data'] {
    return {
        labels: stats?.date.map(date => formatLabel(date)) ?? [],
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
                pointBackgroundColor: 'var(--fd-color-white)',
                pointBorderColor: CHART_COLORS.primaryLine,
                pointBorderWidth: 2,
            },
        ],
    };
}

export function buildNutrientsLineChartData(
    stats: MappedStatistics | null,
    formatLabel: (date: Date) => string,
    translate: (key: string) => string,
): ChartConfiguration<'line'>['data'] {
    const nutrients = stats?.nutrientsStatistic;

    return {
        labels: stats?.date.map(date => formatLabel(date)) ?? [],
        datasets: [
            {
                data: nutrients?.proteins ?? [],
                label: translate('NUTRIENTS.PROTEINS'),
                borderColor: CHART_COLORS.proteins,
                backgroundColor: CHART_COLORS.proteins,
                tension: 0.3,
                fill: false,
                spanGaps: true,
                pointBackgroundColor: 'var(--fd-color-white)',
                pointBorderColor: CHART_COLORS.proteins,
                pointBorderWidth: 2,
                pointRadius: 4,
            },
            {
                data: nutrients?.fats ?? [],
                label: translate('NUTRIENTS.FATS'),
                borderColor: CHART_COLORS.fats,
                backgroundColor: CHART_COLORS.fats,
                tension: 0.3,
                fill: false,
                spanGaps: true,
                pointBackgroundColor: 'var(--fd-color-white)',
                pointBorderColor: CHART_COLORS.fats,
                pointBorderWidth: 2,
                pointRadius: 4,
            },
            {
                data: nutrients?.carbs ?? [],
                label: translate('NUTRIENTS.CARBS'),
                borderColor: CHART_COLORS.carbs,
                backgroundColor: CHART_COLORS.carbs,
                tension: 0.3,
                fill: false,
                spanGaps: true,
                pointBackgroundColor: 'var(--fd-color-white)',
                pointBorderColor: CHART_COLORS.carbs,
                pointBorderWidth: 2,
                pointRadius: 4,
            },
        ],
    };
}

export function buildNutrientsPieChartData(
    stats: MappedStatistics | null,
    translate: (key: string) => string,
): ChartConfiguration<'pie'>['data'] {
    const aggregated = stats?.aggregatedNutrients;

    return {
        labels: [translate('NUTRIENTS.PROTEINS'), translate('NUTRIENTS.FATS'), translate('NUTRIENTS.CARBS')],
        datasets: [
            {
                data: [aggregated?.proteins ?? 0, aggregated?.fats ?? 0, aggregated?.carbs ?? 0],
                backgroundColor: [CHART_COLORS.proteins, CHART_COLORS.fats, CHART_COLORS.carbs],
                borderWidth: 0,
            },
        ],
    };
}

export function buildNutrientsRadarChartData(
    stats: MappedStatistics | null,
    translate: (key: string) => string,
): ChartConfiguration<'radar'>['data'] {
    const aggregated = stats?.aggregatedNutrients;

    return {
        labels: [translate('NUTRIENTS.PROTEINS'), translate('NUTRIENTS.FATS'), translate('NUTRIENTS.CARBS')],
        datasets: [
            {
                data: [aggregated?.proteins ?? 0, aggregated?.fats ?? 0, aggregated?.carbs ?? 0],
                backgroundColor: CHART_COLORS.radarBackground,
                borderColor: CHART_COLORS.radarBorder,
                borderWidth: 2,
                pointBackgroundColor: CHART_COLORS.primaryLine,
            },
        ],
    };
}

export function buildNutrientsBarChartData(
    stats: MappedStatistics | null,
    translate: (key: string) => string,
): ChartConfiguration<'bar'>['data'] {
    const aggregated = stats?.aggregatedNutrients;

    return {
        labels: [
            translate('NUTRIENTS.PROTEINS'),
            translate('NUTRIENTS.FATS'),
            translate('NUTRIENTS.CARBS'),
            translate('SHARED.NUTRIENTS_SUMMARY.FIBER'),
        ],
        datasets: [
            {
                data: [aggregated?.proteins ?? 0, aggregated?.fats ?? 0, aggregated?.carbs ?? 0, aggregated?.fiber ?? 0],
                backgroundColor: [CHART_COLORS.proteins, CHART_COLORS.fats, CHART_COLORS.carbs, CHART_COLORS.fiber],
                borderRadius: 6,
            },
        ],
    };
}

export function buildBodyChartData<T extends { startDate: string }>(
    points: T[],
    getValue: (point: T) => number | null | undefined,
    formatLabel: (dateString: string) => string,
): ChartConfiguration<'line'>['data'] | null {
    if (points.length === 0) {
        return null;
    }

    const labels: string[] = [];
    const data: Array<number | null> = [];

    points.forEach(point => {
        labels.push(formatLabel(point.startDate));
        const value = getValue(point);
        if (value === undefined || value === null || Number.isNaN(value) || value <= 0) {
            data.push(null);
        } else {
            data.push(Number(value.toFixed(2)));
        }
    });

    if (data.every(value => value === null)) {
        return null;
    }

    const chartData = interpolateMissingBodyValues(data);

    return {
        labels,
        datasets: [
            {
                data: chartData,
                borderColor: CHART_COLORS.primaryLine,
                backgroundColor: 'transparent',
                tension: 0.3,
                pointRadius: 4,
                borderWidth: 2,
                spanGaps: true,
                fill: false,
                pointBackgroundColor: 'var(--fd-color-white)',
                pointBorderColor: CHART_COLORS.primaryLine,
                pointBorderWidth: 2,
            },
        ],
    };
}

function interpolateMissingBodyValues(data: Array<number | null>): Array<number | null> {
    const result = [...data];
    const knownIndexes = result.reduce<number[]>((indexes, value, index) => {
        if (value !== null) {
            indexes.push(index);
        }

        return indexes;
    }, []);

    for (let index = 0; index < knownIndexes.length - 1; index++) {
        const startIndex = knownIndexes[index];
        const endIndex = knownIndexes[index + 1];
        const startValue = result[startIndex];
        const endValue = result[endIndex];

        if (startValue === null || endValue === null || endIndex - startIndex <= 1) {
            continue;
        }

        const step = (endValue - startValue) / (endIndex - startIndex);
        for (let fillIndex = startIndex + 1; fillIndex < endIndex; fillIndex++) {
            result[fillIndex] = Number((startValue + step * (fillIndex - startIndex)).toFixed(2));
        }
    }

    return result;
}

export function buildSummaryMetrics(stats: MappedStatistics | null): SummaryMetrics | null {
    if (stats === null) {
        return null;
    }

    const totalCalories = stats.calories.reduce((sum, value) => sum + value, 0);
    const entries = stats.calories.length > 0 ? stats.calories.length : 1;
    const averageCalories = totalCalories / entries;
    const aggregated = stats.aggregatedNutrients;

    return {
        totalCalories,
        averageCard: {
            consumption: averageCalories,
            steps: 0,
            burned: 0,
        },
        macros: [
            {
                key: 'proteins' as const,
                labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
                value: aggregated.proteins,
                color: CHART_COLORS.proteins,
            },
            {
                key: 'fats' as const,
                labelKey: 'GENERAL.NUTRIENTS.FAT',
                value: aggregated.fats,
                color: CHART_COLORS.fats,
            },
            {
                key: 'carbs' as const,
                labelKey: 'GENERAL.NUTRIENTS.CARB',
                value: aggregated.carbs,
                color: CHART_COLORS.carbs,
            },
            {
                key: 'fiber' as const,
                labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
                value: aggregated.fiber,
                color: CHART_COLORS.fiber,
            },
        ],
    };
}

export function buildMacroSparklineData(
    stats: MappedStatistics | null,
    formatLabel: (date: Date) => string,
): Record<MacroKey, ChartConfiguration<'line'>['data']> {
    const labels = stats?.date.map(date => formatLabel(date)) ?? [];
    const nutrients = stats?.nutrientsStatistic;

    const buildData = (
        series: number[] | undefined,
        color: string,
        fillAlpha = DEFAULT_FILL_ALPHA,
    ): ChartConfiguration<'line'>['data'] => ({
        labels,
        datasets: [
            {
                data: series ?? [],
                borderColor: color,
                backgroundColor: applyAlpha(color, fillAlpha),
                tension: 0.35,
                borderWidth: 2,
                fill: true,
                pointRadius: 0,
                spanGaps: true,
            },
        ],
    });

    return {
        proteins: buildData(nutrients?.proteins, CHART_COLORS.proteins, MACRO_SPARKLINE_FILL_ALPHA),
        fats: buildData(nutrients?.fats, CHART_COLORS.fats, MACRO_SPARKLINE_FILL_ALPHA),
        carbs: buildData(nutrients?.carbs, CHART_COLORS.carbs, MACRO_SPARKLINE_FILL_ALPHA),
        fiber: buildData(nutrients?.fiber, CHART_COLORS.fiber, MACRO_SPARKLINE_FILL_ALPHA),
    };
}

export function buildSummarySparklineData(
    stats: MappedStatistics | null,
    formatLabel: (date: Date) => string,
): ChartConfiguration<'line'>['data'] {
    return {
        labels: stats?.date.map(date => formatLabel(date)) ?? [],
        datasets: [
            {
                data: stats?.calories ?? [],
                borderColor: CHART_COLORS.primaryLine,
                backgroundColor: CHART_COLORS.primaryFill,
                tension: 0.3,
                borderWidth: 2,
                fill: true,
                pointRadius: 0,
                spanGaps: true,
            },
        ],
    };
}
