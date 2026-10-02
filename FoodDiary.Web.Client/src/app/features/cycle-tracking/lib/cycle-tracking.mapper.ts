import { MAX_SYMPTOM_VALUE, MIN_SYMPTOM_VALUE } from './cycle-tracking.config';

const DAY_END_HOURS = 23;
const DAY_END_MINUTES = 59;
const DAY_END_SECONDS = 59;
const DAY_END_MILLISECONDS = 999;
const ISO_DATE_KEY_LENGTH = 10;
const ISO_YEAR_LENGTH = 4;
const SERVER_NOTE_WHITE_SPACE = /\p{White_Space}/u;

export function clampCycleSymptom(value: number | null | undefined): number {
    if (value === null || value === undefined || Number.isNaN(value)) {
        return MIN_SYMPTOM_VALUE;
    }

    return Math.min(MAX_SYMPTOM_VALUE, Math.max(MIN_SYMPTOM_VALUE, value));
}

export function toNullableCycleNumber(value: number | string | null | undefined): number | null {
    if (value === null || value === undefined || value === '') {
        return null;
    }

    const numberValue = Number(value);
    return Number.isNaN(numberValue) ? null : numberValue;
}

export function toOptionalCycleText(value: string | null | undefined): string | undefined {
    const trimmed = value?.trim();
    return trimmed === undefined || trimmed.length === 0 ? undefined : trimmed;
}

export function getCycleNotesLength(value: string | null | undefined): number {
    // Match the payload's JavaScript trim followed by the server's Unicode whitespace trim.
    const notes = toOptionalCycleText(value) ?? '';
    let start = 0;
    let end = notes.length;
    while (start < end && SERVER_NOTE_WHITE_SPACE.test(notes.charAt(start))) {
        start++;
    }
    while (end > start && SERVER_NOTE_WHITE_SPACE.test(notes.charAt(end - 1))) {
        end--;
    }
    return end - start;
}

export function normalizeCycleStartOfDay(value: Date): Date {
    const result = new Date(value);
    result.setHours(0, 0, 0, 0);
    return result;
}

export function normalizeCycleEndOfDay(value: Date): Date {
    const result = new Date(value);
    result.setHours(DAY_END_HOURS, DAY_END_MINUTES, DAY_END_SECONDS, DAY_END_MILLISECONDS);
    return result;
}

export function toCycleDateKey(value: string): string {
    const calendarDateMatch = /^(\d{4}-\d{2}-\d{2})/.exec(value);
    if (calendarDateMatch !== null) {
        return calendarDateMatch[1];
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '';
    }

    const year = date.getFullYear().toString().padStart(ISO_YEAR_LENGTH, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`.slice(0, ISO_DATE_KEY_LENGTH);
}
