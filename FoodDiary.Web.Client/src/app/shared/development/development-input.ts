import { formatDateInputValue, parseLocalDateInputValue } from '../lib/local-date.utils';
import { isRecord } from '../lib/unknown-value.utils';
import { DevelopmentToolError } from './development-tools';
const CALENDAR_DATE_LENGTH = 10;
const MAX_FILTER_LENGTH = 200;

export function devRecord(input: unknown, keys: readonly string[]): Record<string, unknown> {
    if (!isRecord(input) || Object.keys(input).some(key => !keys.includes(key))) {
        throw new DevelopmentToolError('Unexpected arguments. Read the current capability schema.');
    }
    return input;
}

export function devText(input: unknown, maxLength: number, allowEmpty = false): string {
    if (typeof input !== 'string' || input.length > maxLength || (!allowEmpty && input.trim().length === 0)) {
        throw new DevelopmentToolError('Invalid text value.');
    }
    return input;
}

export function devNumber(input: unknown, min: number, max: number): number {
    if (typeof input !== 'number' || !Number.isFinite(input) || input < min || input > max) {
        throw new DevelopmentToolError('Number is outside the supported range.');
    }
    return input;
}

export function devEnum<T extends string>(input: unknown, values: readonly T[]): T {
    const value = values.find(candidate => candidate === input);
    if (value === undefined) {
        throw new DevelopmentToolError('Unsupported option.');
    }
    return value;
}

export function devDay(input: unknown): string {
    const value = devText(input, CALENDAR_DATE_LENGTH);
    const date = /^\d{4}-\d{2}-\d{2}$/.test(value) ? parseLocalDateInputValue(value) : null;
    if (date === null || formatDateInputValue(date) !== value) {
        throw new DevelopmentToolError('Expected a valid calendar date YYYY-MM-DD.');
    }
    return value;
}

export const DEV_OBJECT_SCHEMA = { type: 'object', additionalProperties: true };
export const DEV_EMPTY_SCHEMA = { type: 'object', properties: {}, additionalProperties: false };

/** Replace a published URL filter set; the owning query decoder applies its normal policies. */
export function devQuery(input: unknown, keys: readonly string[]): Record<string, string> {
    const value = devRecord(input, keys);
    const params: Record<string, string> = {};
    for (const [key, raw] of Object.entries(value)) {
        if (raw === null) {
            continue;
        }
        if (typeof raw !== 'string' && typeof raw !== 'boolean' && (typeof raw !== 'number' || !Number.isFinite(raw))) {
            throw new DevelopmentToolError('Filters must contain finite scalar values or null.');
        }
        const text = String(raw);
        if (text.length > MAX_FILTER_LENGTH) {
            throw new DevelopmentToolError('Filter value is too long.');
        }
        params[key] = text;
    }
    return params;
}
