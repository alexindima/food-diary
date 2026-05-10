const END_OF_DAY_HOUR = 23;
const END_OF_DAY_MINUTE = 59;
const END_OF_DAY_SECOND = 59;
const END_OF_DAY_MILLISECOND = 999;

export function normalizeStartOfLocalDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0, 0);
}

export function normalizeEndOfLocalDay(date: Date): Date {
    return new Date(
        date.getFullYear(),
        date.getMonth(),
        date.getDate(),
        END_OF_DAY_HOUR,
        END_OF_DAY_MINUTE,
        END_OF_DAY_SECOND,
        END_OF_DAY_MILLISECOND,
    );
}

export function toLocalDayStartIso(date: Date | null | undefined): string | undefined {
    return date !== null && date !== undefined ? normalizeStartOfLocalDay(date).toISOString() : undefined;
}

export function toLocalDayEndIso(date: Date | null | undefined): string | undefined {
    return date !== null && date !== undefined ? normalizeEndOfLocalDay(date).toISOString() : undefined;
}
