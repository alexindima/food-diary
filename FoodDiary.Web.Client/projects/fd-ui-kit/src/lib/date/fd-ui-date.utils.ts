const DATE_INPUT_PART_LENGTH = 2;
const DATE_INPUT_MONTH_OFFSET = 1;
const DATE_INPUT_YEAR_INDEX = 1;
const DATE_INPUT_MONTH_INDEX = 2;
const DATE_INPUT_DAY_INDEX = 3;
const DATE_TIME_INPUT_HOUR_INDEX = 4;
const DATE_TIME_INPUT_MINUTE_INDEX = 5;

export type FdUiDateValue = Date | string | null | undefined;

export function fdUiParseLocalDate(value: FdUiDateValue): Date | null {
    if (value === null || value === undefined || value === '') {
        return null;
    }

    if (value instanceof Date) {
        return Number.isNaN(value.getTime()) ? null : fdUiStartOfLocalDay(value);
    }

    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
    if (match !== null) {
        const year = Number(match[DATE_INPUT_YEAR_INDEX]);
        const month = Number(match[DATE_INPUT_MONTH_INDEX]);
        const day = Number(match[DATE_INPUT_DAY_INDEX]);
        return new Date(year, month - DATE_INPUT_MONTH_OFFSET, day);
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : fdUiStartOfLocalDay(parsed);
}

export function fdUiParseLocalDateTime(value: FdUiDateValue): Date | null {
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

export function fdUiStartOfLocalDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

export function fdUiStartOfLocalMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
}

export function fdUiAddLocalDays(date: Date, days: number): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

export function fdUiAddLocalMonths(date: Date, months: number): Date {
    return new Date(date.getFullYear(), date.getMonth() + months, date.getDate());
}

export function fdUiFormatDateInputValue(date: Date): string {
    const normalized = fdUiStartOfLocalDay(date);
    const year = normalized.getFullYear();
    const month = fdUiPadDatePart(normalized.getMonth() + DATE_INPUT_MONTH_OFFSET);
    const day = fdUiPadDatePart(normalized.getDate());
    return `${year}-${month}-${day}`;
}

export function fdUiFormatTimeInputValue(date: Date): string {
    return `${fdUiPadDatePart(date.getHours())}:${fdUiPadDatePart(date.getMinutes())}`;
}

export function fdUiPadDatePart(value: number): string {
    return value.toString().padStart(DATE_INPUT_PART_LENGTH, '0');
}
