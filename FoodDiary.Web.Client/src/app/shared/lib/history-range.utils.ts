import { formatDateInputValue, normalizeEndOfUtcDay, normalizeStartOfUtcDay } from './local-date.utils';
import { MS_PER_DAY } from './time.constants';

const DEFAULT_MONTH_OFFSET = 1;
const WEEK_DAYS = 7;

export type HistoryRange = 'week' | 'month' | 'year' | 'custom';

export type HistoryDateRange = {
    start: Date;
    end: Date;
};

export type HistoryCustomRange = {
    start: Date | null;
    end: Date | null;
};

export type HistoryEntryFilters = {
    dateFrom: string;
    dateTo: string;
    sort: 'desc';
    limit: number;
};

export type HistorySummaryFilters = {
    dateFrom: string;
    dateTo: string;
    quantizationDays: number;
};

export type HistoryFilterConfig = {
    entriesLimitMax: number;
    entriesLimitPerDay: number;
    monthQuantizationDays: number;
    yearQuantizationDays: number;
    customQuantizationDivisor: number;
};

export function isHistoryRange(value: string): value is HistoryRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}

export function calculateHistoryRangeDates(
    range: HistoryRange,
    customRange: HistoryCustomRange | null | undefined,
    now = new Date(),
): HistoryDateRange {
    switch (range) {
        case 'week': {
            return buildOffsetRange(now, date => date.setDate(date.getDate() - WEEK_DAYS));
        }
        case 'month': {
            return buildOffsetRange(now, date => date.setMonth(date.getMonth() - DEFAULT_MONTH_OFFSET));
        }
        case 'year': {
            return buildOffsetRange(now, date => date.setFullYear(date.getFullYear() - DEFAULT_MONTH_OFFSET));
        }
        case 'custom': {
            return buildCustomRange(customRange, now);
        }
    }
}

export function buildDefaultHistoryCustomRange(now = new Date()): HistoryDateRange {
    return buildOffsetRange(now, date => date.setMonth(date.getMonth() - DEFAULT_MONTH_OFFSET));
}

export function buildHistoryFiltersForRange(
    range: HistoryRange,
    customRange: HistoryCustomRange | null | undefined,
    config: HistoryFilterConfig,
): {
    entriesParams: HistoryEntryFilters;
    summaryParams: HistorySummaryFilters;
    rangeKey: string;
} {
    const { start, end } = calculateHistoryRangeDates(range, customRange);
    const normalizedStart = normalizeStartOfHistoryDay(start);
    const normalizedEnd = normalizeEndOfHistoryDay(end);
    const dateFrom = normalizedStart.toISOString();
    const dateTo = normalizedEnd.toISOString();
    const totalDays = Math.max(1, Math.ceil((normalizedEnd.getTime() - normalizedStart.getTime()) / MS_PER_DAY));
    const quantizationDays = getHistoryQuantizationDays(range, totalDays, config);

    return {
        entriesParams: {
            dateFrom,
            dateTo,
            sort: 'desc',
            limit: Math.min(config.entriesLimitMax, totalDays * config.entriesLimitPerDay),
        },
        summaryParams: {
            dateFrom,
            dateTo,
            quantizationDays,
        },
        rangeKey: `${dateFrom}_${dateTo}`,
    };
}

export function normalizeStartOfHistoryDay(date: Date): Date {
    return normalizeStartOfUtcDay(date);
}

export function normalizeEndOfHistoryDay(date: Date): Date {
    return normalizeEndOfUtcDay(date);
}

export function formatHistoryDateInput(date: Date): string {
    return formatDateInputValue(date);
}

function buildOffsetRange(now: Date, applyOffset: (date: Date) => void): HistoryDateRange {
    const end = new Date(now);
    const start = new Date(now);
    applyOffset(start);
    return { start, end };
}

function buildCustomRange(customRange: HistoryCustomRange | null | undefined, now: Date): HistoryDateRange {
    const fallback = buildDefaultHistoryCustomRange(now);

    return {
        start: customRange?.start !== null && customRange?.start !== undefined ? new Date(customRange.start) : fallback.start,
        end: customRange?.end !== null && customRange?.end !== undefined ? new Date(customRange.end) : fallback.end,
    };
}

function getHistoryQuantizationDays(range: HistoryRange, totalDays: number, config: HistoryFilterConfig): number {
    switch (range) {
        case 'week': {
            return 1;
        }
        case 'month': {
            return config.monthQuantizationDays;
        }
        case 'year': {
            return config.yearQuantizationDays;
        }
        case 'custom': {
            return Math.max(1, Math.round(totalDays / config.customQuantizationDivisor));
        }
    }
}
