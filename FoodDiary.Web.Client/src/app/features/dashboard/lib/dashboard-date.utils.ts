import { normalizeEndOfUtcDay, normalizeStartOfLocalDay, normalizeStartOfUtcDay } from '../../../shared/lib/local-date.utils';

const HYDRATION_DAY_HOUR = 12;
const START_OF_DAY_MINUTE = 0;
const START_OF_DAY_SECOND = 0;

export function normalizeDate(date: Date): Date {
    return normalizeStartOfLocalDay(date);
}

export function getDashboardDateUtc(date: Date): Date {
    return normalizeStartOfUtcDay(date);
}

export function getHydrationDateUtc(date: Date): Date {
    return new Date(
        Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), HYDRATION_DAY_HOUR, START_OF_DAY_MINUTE, START_OF_DAY_SECOND),
    );
}

export function normalizeStartOfDayUtc(date: Date): Date {
    return normalizeStartOfUtcDay(date);
}

export function normalizeEndOfDayUtc(date: Date): Date {
    return normalizeEndOfUtcDay(date);
}

export function getWeightTrendRange(selectedDate: Date, trendDays: number): { start: Date; end: Date } {
    const end = selectedDate;
    const start = new Date(end);
    start.setDate(start.getDate() - (trendDays - 1));

    return {
        start: normalizeStartOfDayUtc(start),
        end: normalizeEndOfDayUtc(end),
    };
}
