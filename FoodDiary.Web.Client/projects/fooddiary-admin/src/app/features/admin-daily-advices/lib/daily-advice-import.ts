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

function isPair(value: unknown): boolean {
    if (
        !isRecord(value) ||
        typeof value['id'] !== 'string' ||
        !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value['id']) ||
        value['id'] === '00000000-0000-0000-0000-000000000000'
    ) {
        return false;
    }
    return (
        isAdvice({ value: value['ru'], locale: 'ru', weight: value['weight'], tag: value['tag'] }) &&
        isAdvice({ value: value['en'], locale: 'en', weight: value['weight'], tag: value['tag'] })
    );
}

export function isDailyAdviceImport(value: unknown): value is AdminDailyAdvicesImportRequest {
    if (!isRecord(value) || !Array.isArray(value['advices']) || value['advices'].length === 0) {
        return false;
    }
    if (value['version'] === 1) {
        return value['advices'].length <= MAX_ADVICE_COUNT && value['advices'].every(isAdvice);
    }
    if (value['version'] !== 2 || value['advices'].length > MAX_ADVICE_COUNT / 2 || !value['advices'].every(isPair)) {
        return false;
    }
    const ids = value['advices'].map((item: Record<string, unknown>) => String(item['id']).toLowerCase());
    return new Set(ids).size === ids.length;
}

export const DAILY_ADVICE_IMPORT_EXAMPLE: AdminDailyAdvicesImportRequest = {
    version: 2,
    advices: [
        {
            id: 'dfd9ce3d-dba5-45c5-a4f0-b3277f282d33',
            ru: 'Пейте воду в течение дня.',
            en: 'Drink water throughout the day.',
            weight: 1,
            tag: 'hydration',
        },
    ],
};
