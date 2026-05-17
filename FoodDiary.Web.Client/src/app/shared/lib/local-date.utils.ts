export const END_OF_DAY_HOUR = 23;
export const END_OF_DAY_MINUTE = 59;
export const END_OF_DAY_SECOND = 59;
export const END_OF_DAY_MILLISECOND = 999;

const START_OF_DAY_HOUR = 0;
const START_OF_DAY_MINUTE = 0;
const START_OF_DAY_SECOND = 0;
const START_OF_DAY_MILLISECOND = 0;
const DATE_INPUT_PART_LENGTH = 2;
const DATE_INPUT_MONTH_OFFSET = 1;
const DATE_INPUT_YEAR_INDEX = 1;
const DATE_INPUT_MONTH_INDEX = 2;
const DATE_INPUT_DAY_INDEX = 3;
const DATE_TIME_INPUT_HOUR_INDEX = 4;
const DATE_TIME_INPUT_MINUTE_INDEX = 5;

export type DateInputValue = Date | string | null | undefined;

export function parseDateValue(value: DateInputValue): Date | null {
    if (value === null || value === undefined || value === '') {
        return null;
    }

    if (value instanceof Date) {
        return Number.isNaN(value.getTime()) ? null : value;
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
}

export function parseLocalDateInputValue(value: DateInputValue): Date | null {
    if (value === null || value === undefined || value === '') {
        return null;
    }

    if (value instanceof Date) {
        return Number.isNaN(value.getTime()) ? null : normalizeStartOfLocalDay(value);
    }

    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
    if (match !== null) {
        const year = Number(match[DATE_INPUT_YEAR_INDEX]);
        const month = Number(match[DATE_INPUT_MONTH_INDEX]);
        const day = Number(match[DATE_INPUT_DAY_INDEX]);
        return new Date(year, month - DATE_INPUT_MONTH_OFFSET, day);
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : normalizeStartOfLocalDay(parsed);
}

export function parseLocalDateTimeInputValue(value: DateInputValue): Date | null {
    if (value === null || value === undefined || value === '') {
        return null;
    }

    if (value instanceof Date) {
        return Number.isNaN(value.getTime()) ? null : value;
    }

    const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/.exec(value);
    if (match !== null) {
        const year = Number(match[DATE_INPUT_YEAR_INDEX]);
        const month = Number(match[DATE_INPUT_MONTH_INDEX]);
        const day = Number(match[DATE_INPUT_DAY_INDEX]);
        const hours = Number(match[DATE_TIME_INPUT_HOUR_INDEX]);
        const minutes = Number(match[DATE_TIME_INPUT_MINUTE_INDEX]);
        return new Date(year, month - DATE_INPUT_MONTH_OFFSET, day, hours, minutes);
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
}

export function normalizeStartOfLocalDay(date: Date): Date {
    return new Date(
        date.getFullYear(),
        date.getMonth(),
        date.getDate(),
        START_OF_DAY_HOUR,
        START_OF_DAY_MINUTE,
        START_OF_DAY_SECOND,
        START_OF_DAY_MILLISECOND,
    );
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

export function normalizeStartOfUtcDay(date: Date): Date {
    return new Date(
        Date.UTC(
            date.getFullYear(),
            date.getMonth(),
            date.getDate(),
            START_OF_DAY_HOUR,
            START_OF_DAY_MINUTE,
            START_OF_DAY_SECOND,
            START_OF_DAY_MILLISECOND,
        ),
    );
}

export function normalizeEndOfUtcDay(date: Date): Date {
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

export function toLocalDayStartIso(date: Date | null | undefined): string | undefined {
    return date !== null && date !== undefined ? normalizeStartOfLocalDay(date).toISOString() : undefined;
}

export function toLocalDayEndIso(date: Date | null | undefined): string | undefined {
    return date !== null && date !== undefined ? normalizeEndOfLocalDay(date).toISOString() : undefined;
}

export function toUtcDayStartIso(date: Date | null | undefined): string | undefined {
    return date !== null && date !== undefined ? normalizeStartOfUtcDay(date).toISOString() : undefined;
}

export function toUtcDayEndIso(date: Date | null | undefined): string | undefined {
    return date !== null && date !== undefined ? normalizeEndOfUtcDay(date).toISOString() : undefined;
}

export function formatDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = padDatePart(date.getMonth() + DATE_INPUT_MONTH_OFFSET);
    const day = padDatePart(date.getDate());
    return `${year}-${month}-${day}`;
}

export function formatTimeInputValue(date: Date): string {
    return `${padDatePart(date.getHours())}:${padDatePart(date.getMinutes())}`;
}

export function formatDateValue(value: DateInputValue, locale: string, options?: Intl.DateTimeFormatOptions): string | null {
    const date = parseDateValue(value);
    return date !== null ? new Intl.DateTimeFormat(locale, options).format(date) : null;
}

export function getDateTimestamp(value: Date | null | undefined): number | null {
    return value?.getTime() ?? null;
}

export function compareDatesAsc(left: DateInputValue, right: DateInputValue): number {
    return getRequiredTimestamp(left) - getRequiredTimestamp(right);
}

export function compareDatesDesc(left: DateInputValue, right: DateInputValue): number {
    return getRequiredTimestamp(right) - getRequiredTimestamp(left);
}

function getRequiredTimestamp(value: DateInputValue): number {
    const date = parseDateValue(value);
    return date?.getTime() ?? 0;
}

function padDatePart(value: number): string {
    return value.toString().padStart(DATE_INPUT_PART_LENGTH, '0');
}
