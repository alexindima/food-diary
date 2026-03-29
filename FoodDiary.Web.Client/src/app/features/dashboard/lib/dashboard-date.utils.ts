export function normalizeDate(date: Date): Date {
    const normalized = new Date(date);
    normalized.setHours(0, 0, 0, 0);
    return normalized;
}

export function getDashboardDateUtc(date: Date): Date {
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
}

export function getHydrationDateUtc(date: Date): Date {
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 12, 0, 0));
}

export function normalizeStartOfDayUtc(date: Date): Date {
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
}

export function normalizeEndOfDayUtc(date: Date): Date {
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999));
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
