import { END_OF_DAY_HOUR, END_OF_DAY_MILLISECOND, END_OF_DAY_MINUTE, END_OF_DAY_SECOND } from '../../../shared/lib/local-date.utils';
import { MS_PER_DAY } from '../../../shared/lib/time.constants';
import type { WaistEntryFilters, WaistEntrySummaryFilters } from '../models/waist-entry.data';
import { WAIST_HISTORY_ENTRIES_LIMIT_MAX } from './waist-history.constants';
import type { WaistHistoryCustomRange, WaistHistoryDateRange, WaistHistoryRange } from './waist-history.types';

const DEFAULT_MONTH_OFFSET = 1;
const WEEK_DAYS = 7;
const ENTRIES_LIMIT_PER_DAY = 5;
const MONTH_QUANTIZATION_DAYS = 3;
const YEAR_QUANTIZATION_DAYS = 14;
const CUSTOM_QUANTIZATION_DIVISOR = 12;
const DATE_PART_PAD_LENGTH = 2;
const DATE_PART_PAD = '0';

export function isWaistHistoryRange(value: string): value is WaistHistoryRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}

export function calculateWaistHistoryRangeDates(
    range: WaistHistoryRange,
    customRange: WaistHistoryCustomRange | null,
    now = new Date(),
): WaistHistoryDateRange {
    if (range === 'week') {
        return buildOffsetRange(now, date => date.setDate(date.getDate() - WEEK_DAYS));
    }

    if (range === 'month') {
        return buildOffsetRange(now, date => date.setMonth(date.getMonth() - DEFAULT_MONTH_OFFSET));
    }

    if (range === 'year') {
        return buildOffsetRange(now, date => date.setFullYear(date.getFullYear() - DEFAULT_MONTH_OFFSET));
    }

    return buildCustomRange(customRange, now);
}

export function buildDefaultWaistHistoryCustomRange(now = new Date()): WaistHistoryDateRange {
    const end = new Date(now);
    const start = new Date(now);
    start.setMonth(start.getMonth() - DEFAULT_MONTH_OFFSET);
    return { start, end };
}

export function buildWaistHistoryFiltersForRange(
    range: WaistHistoryRange,
    customRange: WaistHistoryCustomRange | null,
): {
    entriesParams: WaistEntryFilters;
    summaryParams: WaistEntrySummaryFilters;
    rangeKey: string;
} {
    const { start, end } = calculateWaistHistoryRangeDates(range, customRange);
    const normalizedStart = normalizeStartOfDay(start);
    const normalizedEnd = normalizeEndOfDay(end);
    const totalDays = Math.max(1, Math.ceil((normalizedEnd.getTime() - normalizedStart.getTime()) / MS_PER_DAY));
    const quantizationDays = getWaistHistoryQuantizationDays(range, totalDays);
    const limit = Math.min(WAIST_HISTORY_ENTRIES_LIMIT_MAX, totalDays * ENTRIES_LIMIT_PER_DAY);
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

export function formatWaistHistoryDateInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(DATE_PART_PAD_LENGTH, DATE_PART_PAD);
    const day = String(date.getDate()).padStart(DATE_PART_PAD_LENGTH, DATE_PART_PAD);
    return `${year}-${month}-${day}`;
}

function buildOffsetRange(now: Date, applyOffset: (date: Date) => void): WaistHistoryDateRange {
    const end = new Date(now);
    const start = new Date(now);
    applyOffset(start);
    return { start, end };
}

function buildCustomRange(customRange: WaistHistoryCustomRange | null, now: Date): WaistHistoryDateRange {
    const fallback = buildDefaultWaistHistoryCustomRange(now);
    const start = customRange?.start;
    const end = customRange?.end;

    return {
        start: start !== undefined && start !== null ? new Date(start) : fallback.start,
        end: end !== undefined && end !== null ? new Date(end) : fallback.end,
    };
}

function getWaistHistoryQuantizationDays(range: WaistHistoryRange, totalDays: number): number {
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
