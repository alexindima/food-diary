import type { FdUiLineChartPoint } from 'fd-ui-kit';

import { normalizeEndOfLocalDay, normalizeStartOfLocalDay } from '../../../shared/lib/local-date.utils';
import { MS_PER_DAY } from '../../../shared/lib/time.constants';

export type StatisticsRange = 'week' | 'month' | 'quarter' | 'halfYear' | 'year' | 'custom';
export type NutritionChartTab = 'calories' | 'macros' | 'distribution';

export type DateRange = {
    start: Date;
    end: Date;
};

export function isStatisticsRange(value: unknown): value is StatisticsRange {
    return value === 'week' || value === 'month' || value === 'quarter' || value === 'halfYear' || value === 'year' || value === 'custom';
}

export function isNutritionTab(value: unknown): value is NutritionChartTab {
    return value === 'calories' || value === 'macros' || value === 'distribution';
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
const QUARTER_MONTH_OFFSET = 3;
const HALF_YEAR_MONTH_OFFSET = 6;
const MINIMUM_DAY_COUNT = 1;

function buildChartPoints(labels: readonly string[], series: ReadonlyArray<number | null> | undefined): FdUiLineChartPoint[] {
    return labels.map((label, index) => ({
        label,
        value: series?.[index] ?? null,
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

// eslint-disable-next-line complexity -- Each supported period has one explicit, calendar-aware adjustment.
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

    if (range === 'quarter') {
        start.setMonth(end.getMonth() - QUARTER_MONTH_OFFSET);
        return { start, end };
    }

    if (range === 'halfYear') {
        start.setMonth(end.getMonth() - HALF_YEAR_MONTH_OFFSET);
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

export function buildBodyChartPoints<T extends { startDate: string }>(
    points: T[],
    getValue: (point: T) => number | null | undefined,
    formatLabel: (dateString: string) => string,
): FdUiLineChartPoint[] {
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
