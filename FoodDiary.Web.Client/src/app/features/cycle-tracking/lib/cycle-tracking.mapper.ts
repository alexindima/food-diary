import { MAX_SYMPTOM_VALUE, MIN_SYMPTOM_VALUE } from './cycle-tracking.config';

const DAY_END_HOURS = 23;
const DAY_END_MINUTES = 59;
const DAY_END_SECONDS = 59;
const DAY_END_MILLISECONDS = 999;
const ISO_DATE_KEY_LENGTH = 10;

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
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, ISO_DATE_KEY_LENGTH);
}
