const START_OF_DAY_HOUR = 0;
const START_OF_DAY_MINUTE = 0;
const START_OF_DAY_SECOND = 0;
const START_OF_DAY_MS = 0;
const HYDRATION_DAY_HOUR = 12;
const END_OF_DAY_HOUR = 23;
const END_OF_DAY_MINUTE = 59;
const END_OF_DAY_SECOND = 59;
const END_OF_DAY_MS = 999;

export function normalizeDate(date: Date): Date {
    const normalized = new Date(date);
    normalized.setHours(START_OF_DAY_HOUR, START_OF_DAY_MINUTE, START_OF_DAY_SECOND, START_OF_DAY_MS);
    return normalized;
}

export function getDashboardDateUtc(date: Date): Date {
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
}

export function getHydrationDateUtc(date: Date): Date {
    return new Date(
        Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), HYDRATION_DAY_HOUR, START_OF_DAY_MINUTE, START_OF_DAY_SECOND),
    );
}

export function normalizeStartOfDayUtc(date: Date): Date {
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
}

export function normalizeEndOfDayUtc(date: Date): Date {
    return new Date(
        Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), END_OF_DAY_HOUR, END_OF_DAY_MINUTE, END_OF_DAY_SECOND, END_OF_DAY_MS),
    );
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
