import type { AdminDailyAdvicesImportRequest } from '../models/admin-daily-advice.models';

export const MAX_ADVICE_IMPORT_BYTES = 5242880;
const MAX_ADVICE_COUNT = 1000;
const MAX_VALUE_LENGTH = 512;
const MAX_TAG_LENGTH = 64;
const MAX_LOCALE_LENGTH = 10;
const MAX_WEIGHT = 2147483647;

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isAdvice(value: unknown): boolean {
    if (!isRecord(value) || typeof value['value'] !== 'string' || typeof value['locale'] !== 'string') {
        return false;
    }
    const text = value['value'].trim();
    const locale = value['locale'].trim().toLowerCase();
    const language = locale.split(/[-_]/)[0];
    return (
        text.length > 0 &&
        text.length <= MAX_VALUE_LENGTH &&
        locale.length <= MAX_LOCALE_LENGTH &&
        (language === 'ru' || language === 'en') &&
        isWeight(value['weight']) &&
        isTag(value['tag'])
    );
}

function isWeight(weight: unknown): boolean {
    return weight === undefined || (typeof weight === 'number' && Number.isInteger(weight) && weight > 0 && weight <= MAX_WEIGHT);
}

function isTag(tag: unknown): boolean {
    return tag === undefined || tag === null || (typeof tag === 'string' && tag.trim().length <= MAX_TAG_LENGTH);
}

export function isDailyAdviceImport(value: unknown): value is AdminDailyAdvicesImportRequest {
    return (
        isRecord(value) &&
        value['version'] === 1 &&
        Array.isArray(value['advices']) &&
        value['advices'].length > 0 &&
        value['advices'].length <= MAX_ADVICE_COUNT &&
        value['advices'].every(isAdvice)
    );
}

export const DAILY_ADVICE_IMPORT_EXAMPLE: AdminDailyAdvicesImportRequest = {
    version: 1,
    advices: [
        { value: 'Пейте воду в течение дня.', locale: 'ru', weight: 1, tag: 'hydration' },
        { value: 'Drink water throughout the day.', locale: 'en', weight: 1, tag: 'hydration' },
    ],
};
