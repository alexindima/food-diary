import { normalizeEndOfUtcDay, normalizeStartOfLocalDay, normalizeStartOfUtcDay } from '../../../shared/lib/local-date.utils';

const DATE_YEAR_LENGTH = 4;
const HYDRATION_DAY_HOUR = 12;
const START_OF_DAY_MINUTE = 0;
const START_OF_DAY_SECOND = 0;

export function parseDashboardDate(value: string | null): Date | null {
    if (value === null || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        return null;
    }
    const date = new Date(`${value}T00:00:00`);
    if (Number.isNaN(date.getTime())) {
        return null;
    }
    const formatted = `${date.getFullYear().toString().padStart(DATE_YEAR_LENGTH, '0')}-${(date.getMonth() + 1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')}`;
    return formatted === value ? date : null;
}

export function normalizeDate(date: Date): Date {
    return normalizeStartOfLocalDay(date);
}

export function getDashboardDateUtc(date: Date): Date {
    return normalizeStartOfUtcDay(date);
}

export function getHydrationDateUtc(date: Date, now: Date = new Date()): Date {
    if (normalizeDate(date).getTime() === normalizeDate(now).getTime()) {
        return new Date(now);
    }
    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), HYDRATION_DAY_HOUR, START_OF_DAY_MINUTE, START_OF_DAY_SECOND);
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
