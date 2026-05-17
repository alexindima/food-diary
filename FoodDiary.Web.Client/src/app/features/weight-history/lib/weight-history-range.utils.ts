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
import type { WeightEntryFilters, WeightEntrySummaryFilters } from '../models/weight-entry.data';
import { WEIGHT_HISTORY_ENTRIES_LIMIT_MAX } from './weight-history.constants';
import type { WeightHistoryCustomRange, WeightHistoryDateRange, WeightHistoryRange } from './weight-history.types';

const ENTRIES_LIMIT_PER_DAY = 5;
const MONTH_QUANTIZATION_DAYS = 3;
const YEAR_QUANTIZATION_DAYS = 14;
const CUSTOM_QUANTIZATION_DIVISOR = 12;
const WEIGHT_HISTORY_FILTER_CONFIG: HistoryFilterConfig = {
    entriesLimitMax: WEIGHT_HISTORY_ENTRIES_LIMIT_MAX,
    entriesLimitPerDay: ENTRIES_LIMIT_PER_DAY,
    monthQuantizationDays: MONTH_QUANTIZATION_DAYS,
    yearQuantizationDays: YEAR_QUANTIZATION_DAYS,
    customQuantizationDivisor: CUSTOM_QUANTIZATION_DIVISOR,
};

export function isWeightHistoryRange(value: string): value is WeightHistoryRange {
    return isHistoryRange(value);
}

export function calculateWeightHistoryRangeDates(
    range: WeightHistoryRange,
    customRange: WeightHistoryCustomRange | null | undefined,
    now = new Date(),
): WeightHistoryDateRange {
    return calculateSharedHistoryRangeDates(range, customRange, now);
}

export function buildDefaultWeightHistoryCustomRange(now = new Date()): WeightHistoryDateRange {
    return buildDefaultHistoryCustomRange(now);
}

export function buildWeightHistoryFiltersForRange(
    range: WeightHistoryRange,
    customRange: WeightHistoryCustomRange | null | undefined,
): {
    entriesParams: WeightEntryFilters;
    summaryParams: WeightEntrySummaryFilters;
    rangeKey: string;
} {
    return buildHistoryFiltersForRange(range, customRange, WEIGHT_HISTORY_FILTER_CONFIG);
}

export function normalizeStartOfDay(date: Date): Date {
    return normalizeStartOfHistoryDay(date);
}

export function normalizeEndOfDay(date: Date): Date {
    return normalizeEndOfHistoryDay(date);
}

export function formatWeightHistoryDateInput(date: Date): string {
    return formatHistoryDateInput(date);
}
