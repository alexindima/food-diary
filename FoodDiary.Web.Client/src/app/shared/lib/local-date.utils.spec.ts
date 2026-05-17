import { describe, expect, it } from 'vitest';

import {
    compareDatesAsc,
    compareDatesDesc,
    formatDateInputValue,
    formatDateValue,
    formatTimeInputValue,
    getDateTimestamp,
    normalizeEndOfLocalDay,
    normalizeEndOfUtcDay,
    normalizeStartOfLocalDay,
    normalizeStartOfUtcDay,
    parseDateValue,
    parseLocalDateInputValue,
    parseLocalDateTimeInputValue,
    toLocalDayEndIso,
    toLocalDayStartIso,
    toUtcDayEndIso,
    toUtcDayStartIso,
} from './local-date.utils';

const YEAR = 2026;
const MAY = 4;
const DAY = 5;
const INPUT_HOURS = 14;
const INPUT_MINUTES = 30;
const INPUT_SECONDS = 25;
const INPUT_MS = 123;
const EARLY_HOURS = 7;
const EARLY_MINUTES = 5;
const END_OF_DAY_HOURS = 23;
const END_OF_DAY_MINUTES = 59;
const END_OF_DAY_SECONDS = 59;
const END_OF_DAY_MS = 999;

describe('local-date.utils', () => {
    it('normalizes dates to local day boundaries', () => {
        const date = createInputDate();

        expect(normalizeStartOfLocalDay(date)).toEqual(createStartOfDayDate());
        expect(normalizeEndOfLocalDay(date)).toEqual(createEndOfDayDate());
    });

    it('normalizes dates to UTC day boundaries using local date parts', () => {
        const date = createInputDate();

        expect(normalizeStartOfUtcDay(date).toISOString()).toBe('2026-05-05T00:00:00.000Z');
        expect(normalizeEndOfUtcDay(date).toISOString()).toBe('2026-05-05T23:59:59.999Z');
    });

    it('returns ISO values for local day boundaries', () => {
        const date = createInputDate();

        expect(toLocalDayStartIso(date)).toBe(createStartOfDayDate().toISOString());
        expect(toLocalDayEndIso(date)).toBe(createEndOfDayDate().toISOString());
    });

    it('returns ISO values for UTC day boundaries', () => {
        const date = createInputDate();

        expect(toUtcDayStartIso(date)).toBe('2026-05-05T00:00:00.000Z');
        expect(toUtcDayEndIso(date)).toBe('2026-05-05T23:59:59.999Z');
    });

    it('keeps empty optional dates undefined', () => {
        expect(toLocalDayStartIso(null)).toBeUndefined();
        expect(toLocalDayEndIso(undefined)).toBeUndefined();
        expect(toUtcDayStartIso(null)).toBeUndefined();
        expect(toUtcDayEndIso(undefined)).toBeUndefined();
    });

    it('formats date and time input values', () => {
        const date = new Date(YEAR, MAY, DAY, EARLY_HOURS, EARLY_MINUTES);

        expect(formatDateInputValue(date)).toBe('2026-05-05');
        expect(formatTimeInputValue(date)).toBe('07:05');
    });

    it('formats nullable date values with Intl options', () => {
        expect(formatDateValue(createInputDate(), 'en-US', { year: 'numeric', month: '2-digit', day: '2-digit' })).toBe('05/05/2026');
        expect(formatDateValue('invalid', 'en-US')).toBeNull();
    });

    it('parses generic dates safely', () => {
        expect(parseDateValue('2026-05-05T10:30:00Z')?.toISOString()).toBe('2026-05-05T10:30:00.000Z');
        expect(parseDateValue('')).toBeNull();
        expect(parseDateValue('invalid')).toBeNull();
    });

    it('parses date input values as local dates', () => {
        const parsed = parseLocalDateInputValue('2026-05-05');

        expect(parsed).toEqual(createStartOfDayDate());
        expect(parseLocalDateInputValue('invalid')).toBeNull();
    });

    it('parses date-time input values as local dates', () => {
        const parsed = parseLocalDateTimeInputValue('2026-05-05T07:05');

        expect(parsed).toEqual(new Date(YEAR, MAY, DAY, EARLY_HOURS, EARLY_MINUTES));
        expect(parseLocalDateTimeInputValue('invalid')).toBeNull();
    });

    it('returns nullable timestamps and date comparisons', () => {
        const earlier = new Date(YEAR, MAY, DAY);
        const later = new Date(YEAR, MAY, DAY + 1);

        expect(getDateTimestamp(null)).toBeNull();
        expect(getDateTimestamp(earlier)).toBe(earlier.getTime());
        expect(compareDatesAsc(earlier, later)).toBeLessThan(0);
        expect(compareDatesDesc(earlier, later)).toBeGreaterThan(0);
    });
});

function createInputDate(): Date {
    return new Date(YEAR, MAY, DAY, INPUT_HOURS, INPUT_MINUTES, INPUT_SECONDS, INPUT_MS);
}

function createStartOfDayDate(): Date {
    return new Date(YEAR, MAY, DAY, 0, 0, 0, 0);
}

function createEndOfDayDate(): Date {
    return new Date(YEAR, MAY, DAY, END_OF_DAY_HOURS, END_OF_DAY_MINUTES, END_OF_DAY_SECONDS, END_OF_DAY_MS);
}
