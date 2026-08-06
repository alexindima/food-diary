import {
    buildDefaultHistoryCustomRange,
    buildHistoryFiltersForRange,
    calculateHistoryRangeDates as calculateSharedHistoryRangeDates,
    formatHistoryDateInput,
    type HistoryFilterConfig,
    isHistoryRange,
    normalizeEndOfHistoryDay,
    normalizeStartOfHistoryDay,
} from '../../../shared/lib/history-range.utils';
import type { WaistEntryFilters, WaistEntrySummaryFilters } from '../models/waist-entry.data';
import { WAIST_HISTORY_ENTRIES_LIMIT_MAX } from './waist-history.constants';
import type { WaistHistoryCustomRange, WaistHistoryDateRange, WaistHistoryRange } from './waist-history.types';

const ENTRIES_LIMIT_PER_DAY = 5;
const MONTH_QUANTIZATION_DAYS = 3;
const YEAR_QUANTIZATION_DAYS = 14;
const CUSTOM_QUANTIZATION_DIVISOR = 12;
const WAIST_HISTORY_FILTER_CONFIG: HistoryFilterConfig = {
    entriesLimitMax: WAIST_HISTORY_ENTRIES_LIMIT_MAX,
    entriesLimitPerDay: ENTRIES_LIMIT_PER_DAY,
    monthQuantizationDays: MONTH_QUANTIZATION_DAYS,
    yearQuantizationDays: YEAR_QUANTIZATION_DAYS,
    customQuantizationDivisor: CUSTOM_QUANTIZATION_DIVISOR,
};

export function isWaistHistoryRange(value: string): value is WaistHistoryRange {
    return isHistoryRange(value);
}

export function calculateWaistHistoryRangeDates(
    range: WaistHistoryRange,
    customRange: WaistHistoryCustomRange | null,
    now = new Date(),
): WaistHistoryDateRange {
    return calculateSharedHistoryRangeDates(range, customRange, now);
}

export function buildDefaultWaistHistoryCustomRange(now = new Date()): WaistHistoryDateRange {
    return buildDefaultHistoryCustomRange(now);
}

export function buildWaistHistoryFiltersForRange(
    range: WaistHistoryRange,
    customRange: WaistHistoryCustomRange | null,
): {
    entriesParams: WaistEntryFilters;
    summaryParams: WaistEntrySummaryFilters;
    rangeKey: string;
} {
    return buildHistoryFiltersForRange(range, customRange, WAIST_HISTORY_FILTER_CONFIG);
}

export function normalizeStartOfDay(date: Date): Date {
    return normalizeStartOfHistoryDay(date);
}

export function normalizeEndOfDay(date: Date): Date {
    return normalizeEndOfHistoryDay(date);
}

export function formatWaistHistoryDateInput(date: Date): string {
    return formatHistoryDateInput(date);
}
