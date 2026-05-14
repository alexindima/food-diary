import { END_OF_DAY_HOUR, END_OF_DAY_MILLISECOND, END_OF_DAY_MINUTE, END_OF_DAY_SECOND } from '../../../shared/lib/local-date.utils';
import { MS_PER_DAY } from '../../../shared/lib/time.constants';
import type { WeightEntryFilters, WeightEntrySummaryFilters } from '../models/weight-entry.data';
import { WEIGHT_HISTORY_ENTRIES_LIMIT_MAX } from './weight-history.constants';
import type { WeightHistoryCustomRange, WeightHistoryDateRange, WeightHistoryRange } from './weight-history.types';

const DEFAULT_MONTH_OFFSET = 1;
const WEEK_DAYS = 7;
const ENTRIES_LIMIT_PER_DAY = 5;
const MONTH_QUANTIZATION_DAYS = 3;
const YEAR_QUANTIZATION_DAYS = 14;
const CUSTOM_QUANTIZATION_DIVISOR = 12;
const DATE_PART_PAD_LENGTH = 2;
const DATE_PART_PAD = '0';

export function isWeightHistoryRange(value: string): value is WeightHistoryRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}

export function calculateWeightHistoryRangeDates(
    range: WeightHistoryRange,
    customRange: WeightHistoryCustomRange | null | undefined,
    now = new Date(),
): WeightHistoryDateRange {
    switch (range) {
        case 'week':
            return buildOffsetRange(now, date => date.setDate(date.getDate() - WEEK_DAYS));
        case 'month':
            return buildOffsetRange(now, date => date.setMonth(date.getMonth() - DEFAULT_MONTH_OFFSET));
        case 'year':
            return buildOffsetRange(now, date => date.setFullYear(date.getFullYear() - DEFAULT_MONTH_OFFSET));
        case 'custom':
            return buildCustomRange(customRange, now);
    }
}

export function buildDefaultWeightHistoryCustomRange(now = new Date()): WeightHistoryDateRange {
    const end = new Date(now);
    const start = new Date(end);
    start.setMonth(start.getMonth() - DEFAULT_MONTH_OFFSET);
    return { start, end };
}

export function buildWeightHistoryFiltersForRange(
    range: WeightHistoryRange,
    customRange: WeightHistoryCustomRange | null | undefined,
): {
    entriesParams: WeightEntryFilters;
    summaryParams: WeightEntrySummaryFilters;
    rangeKey: string;
} {
    const { start, end } = calculateWeightHistoryRangeDates(range, customRange);
    const normalizedStart = normalizeStartOfDay(start);
    const normalizedEnd = normalizeEndOfDay(end);
    const totalDays = Math.max(1, Math.ceil((normalizedEnd.getTime() - normalizedStart.getTime()) / MS_PER_DAY));
    const quantizationDays = getWeightHistoryQuantizationDays(range, totalDays);
    const limit = Math.min(WEIGHT_HISTORY_ENTRIES_LIMIT_MAX, totalDays * ENTRIES_LIMIT_PER_DAY);
    const rangeKey = `${normalizedStart.toISOString()}_${normalizedEnd.toISOString()}`;

    return {
        entriesParams: {
            dateFrom: normalizedStart.toISOString(),
            dateTo: normalizedEnd.toISOString(),
            sort: 'desc',
            limit,
        },
        summaryParams: {
            dateFrom: normalizedStart.toISOString(),
            dateTo: normalizedEnd.toISOString(),
            quantizationDays,
        },
        rangeKey,
    };
}

function buildOffsetRange(now: Date, applyOffset: (date: Date) => void): WeightHistoryDateRange {
    const start = new Date(now);
    const end = new Date(now);
    applyOffset(start);
    return { start, end };
}

function buildCustomRange(customRange: WeightHistoryCustomRange | null | undefined, now: Date): WeightHistoryDateRange {
    const fallback = buildOffsetRange(now, date => date.setMonth(date.getMonth() - DEFAULT_MONTH_OFFSET));

    return {
        start: customRange?.start === null || customRange?.start === undefined ? fallback.start : new Date(customRange.start),
        end: customRange?.end === null || customRange?.end === undefined ? fallback.end : new Date(customRange.end),
    };
}

export function normalizeStartOfDay(date: Date): Date {
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
}

export function normalizeEndOfDay(date: Date): Date {
    return new Date(
        Date.UTC(
            date.getFullYear(),
            date.getMonth(),
            date.getDate(),
            END_OF_DAY_HOUR,
            END_OF_DAY_MINUTE,
            END_OF_DAY_SECOND,
            END_OF_DAY_MILLISECOND,
        ),
    );
}

export function formatWeightHistoryDateInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(DATE_PART_PAD_LENGTH, DATE_PART_PAD);
    const day = String(date.getDate()).padStart(DATE_PART_PAD_LENGTH, DATE_PART_PAD);
    return `${year}-${month}-${day}`;
}

function getWeightHistoryQuantizationDays(range: WeightHistoryRange, totalDays: number): number {
    if (range === 'week') {
        return 1;
    }

    if (range === 'month') {
        return MONTH_QUANTIZATION_DAYS;
    }

    if (range === 'year') {
        return YEAR_QUANTIZATION_DAYS;
    }

    return Math.max(1, Math.round(totalDays / CUSTOM_QUANTIZATION_DIVISOR));
}
