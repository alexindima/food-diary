export function normalizeStartOfLocalDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0, 0);
}

export function normalizeEndOfLocalDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999);
}

export function toLocalDayStartIso(date: Date | null | undefined): string | undefined {
    return date ? normalizeStartOfLocalDay(date).toISOString() : undefined;
}

export function toLocalDayEndIso(date: Date | null | undefined): string | undefined {
    return date ? normalizeEndOfLocalDay(date).toISOString() : undefined;
}
