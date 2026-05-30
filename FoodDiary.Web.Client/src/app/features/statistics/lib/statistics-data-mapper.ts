import type { FdUiBarChartItem, FdUiLineChartPoint, FdUiPieChartSegment } from 'fd-ui-kit';

import type { StatisticsBodyChartPoint } from '../../../components/shared/statistics-body/statistics-body';
import type { NutritionTrendGroup } from '../../../components/shared/statistics-nutrition/statistics-nutrition';
import type { SummaryMetrics, SummarySparklinePoint } from '../../../components/shared/statistics-summary/statistics-summary';
import { CHART_COLORS } from '../../../constants/chart-colors';
import { normalizeEndOfLocalDay, normalizeStartOfLocalDay } from '../../../shared/lib/local-date.utils';
import { MS_PER_DAY } from '../../../shared/lib/time.constants';
import type { MappedStatistics } from '../models/statistics.data';

export type StatisticsRange = 'week' | 'month' | 'year' | 'custom';
export type NutritionChartTab = 'calories' | 'macros' | 'distribution';
export type BodyChartTab = 'weight' | 'bmi' | 'waist' | 'whtr';
export type MacroKey = 'proteins' | 'fats' | 'carbs' | 'fiber';

export type DateRange = {
    start: Date;
    end: Date;
};

export function isStatisticsRange(value: unknown): value is StatisticsRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}

export function isNutritionTab(value: unknown): value is NutritionChartTab {
    return value === 'calories' || value === 'macros' || value === 'distribution';
}

export function isBodyTab(value: unknown): value is BodyChartTab {
    return value === 'weight' || value === 'bmi' || value === 'waist' || value === 'whtr';
}

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
const MINIMUM_DAY_COUNT = 1;

function buildChartPoints(labels: readonly string[], series: ReadonlyArray<number | null> | undefined): FdUiLineChartPoint[] {
    return labels.map((label, index) => ({
        label,
        value: series?.[index] ?? null,
    }));
}

function buildSparklinePoints(labels: readonly string[], series: ReadonlyArray<number | null> | undefined): SummarySparklinePoint[] {
    return labels.map((label, index) => ({
        label,
        value: series?.[index] ?? 0,
    }));
}

export function getQuantizationDays(start: Date, end: Date): number {
    const totalDays = Math.max(1, Math.round((end.getTime() - start.getTime()) / MS_PER_DAY));

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

export function getDateRangeDayCount(range: DateRange): number {
    const start = normalizeStartOfDay(range.start);
    const end = normalizeEndOfDay(range.end);

    return Math.max(MINIMUM_DAY_COUNT, Math.round((end.getTime() - start.getTime()) / MS_PER_DAY));
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
        return createOrderedDateRange(customStart, customEnd);
    }

    return { start, end };
}

function createOrderedDateRange(start: Date, end: Date): DateRange {
    return start.getTime() <= end.getTime() ? { start, end } : { start: end, end: start };
}

export function buildCaloriesTrendPoints(stats: MappedStatistics | null, formatLabel: (date: Date) => string): FdUiLineChartPoint[] {
    const labels = stats?.date.map(date => formatLabel(date)) ?? [];

    return buildChartPoints(labels, stats?.calories);
}

export function buildNutrientTrendGroups(
    stats: MappedStatistics | null,
    formatLabel: (date: Date) => string,
    translate: (key: string) => string,
): NutritionTrendGroup[] {
    const labels = stats?.date.map(date => formatLabel(date)) ?? [];
    const nutrients = stats?.nutrientsStatistic;

    return [
        {
            key: 'proteins',
            label: translate('NUTRIENTS.PROTEINS'),
            color: CHART_COLORS.proteins,
            points: buildChartPoints(labels, nutrients?.proteins),
        },
        {
            key: 'fats',
            label: translate('NUTRIENTS.FATS'),
            color: CHART_COLORS.fats,
            points: buildChartPoints(labels, nutrients?.fats),
        },
        {
            key: 'carbs',
            label: translate('NUTRIENTS.CARBS'),
            color: CHART_COLORS.carbs,
            points: buildChartPoints(labels, nutrients?.carbs),
        },
        {
            key: 'fiber',
            label: translate('SHARED.NUTRIENTS_SUMMARY.FIBER'),
            color: CHART_COLORS.fiber,
            points: buildChartPoints(labels, nutrients?.fiber),
        },
    ];
}

export function buildNutrientPieSegments(stats: MappedStatistics | null, translate: (key: string) => string): FdUiPieChartSegment[] {
    const aggregated = stats?.aggregatedNutrients;

    return [
        { label: translate('NUTRIENTS.PROTEINS'), value: aggregated?.proteins ?? 0, color: CHART_COLORS.proteins },
        { label: translate('NUTRIENTS.FATS'), value: aggregated?.fats ?? 0, color: CHART_COLORS.fats },
        { label: translate('NUTRIENTS.CARBS'), value: aggregated?.carbs ?? 0, color: CHART_COLORS.carbs },
    ];
}

export function buildNutrientBarItems(stats: MappedStatistics | null, translate: (key: string) => string): FdUiBarChartItem[] {
    const aggregated = stats?.aggregatedNutrients;

    return [
        { label: translate('NUTRIENTS.PROTEINS'), value: aggregated?.proteins ?? 0, color: CHART_COLORS.proteins },
        { label: translate('NUTRIENTS.FATS'), value: aggregated?.fats ?? 0, color: CHART_COLORS.fats },
        { label: translate('NUTRIENTS.CARBS'), value: aggregated?.carbs ?? 0, color: CHART_COLORS.carbs },
        { label: translate('SHARED.NUTRIENTS_SUMMARY.FIBER'), value: aggregated?.fiber ?? 0, color: CHART_COLORS.fiber },
    ];
}

export function buildBodyChartPoints<T extends { startDate: string }>(
    points: T[],
    getValue: (point: T) => number | null | undefined,
    formatLabel: (dateString: string) => string,
): StatisticsBodyChartPoint[] {
    if (points.length === 0) {
        return [];
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
        return [];
    }

    const chartData = interpolateMissingBodyValues(data);

    return buildChartPoints(labels, chartData);
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

export function buildSummaryMetrics(stats: MappedStatistics | null, dayCount: number): SummaryMetrics | null {
    if (stats === null) {
        return null;
    }

    const totalCalories = stats.calories.reduce((sum, value) => sum + value, 0);
    const effectiveDayCount = Math.max(MINIMUM_DAY_COUNT, dayCount);
    const averageCalories = totalCalories / effectiveDayCount;
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

export function buildMacroSparklinePoints(
    stats: MappedStatistics | null,
    formatLabel: (date: Date) => string,
): Record<MacroKey, SummarySparklinePoint[]> {
    const labels = stats?.date.map(date => formatLabel(date)) ?? [];
    const nutrients = stats?.nutrientsStatistic;

    return {
        proteins: buildSparklinePoints(labels, nutrients?.proteins),
        fats: buildSparklinePoints(labels, nutrients?.fats),
        carbs: buildSparklinePoints(labels, nutrients?.carbs),
        fiber: buildSparklinePoints(labels, nutrients?.fiber),
    };
}

export function buildSummarySparklinePoints(stats: MappedStatistics | null, formatLabel: (date: Date) => string): SummarySparklinePoint[] {
    const labels = stats?.date.map(date => formatLabel(date)) ?? [];

    return buildSparklinePoints(labels, stats?.calories);
}
