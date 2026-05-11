import { describe, expect, it } from 'vitest';

import { normalizeEndOfLocalDay, normalizeStartOfLocalDay, toLocalDayEndIso, toLocalDayStartIso } from './local-date.utils';

const YEAR = 2026;
const MAY = 4;
const DAY = 5;
const INPUT_HOURS = 14;
const INPUT_MINUTES = 30;
const INPUT_SECONDS = 25;
const INPUT_MS = 123;
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

    it('returns ISO values for local day boundaries', () => {
        const date = createInputDate();

        expect(toLocalDayStartIso(date)).toBe(createStartOfDayDate().toISOString());
        expect(toLocalDayEndIso(date)).toBe(createEndOfDayDate().toISOString());
    });

    it('keeps empty optional dates undefined', () => {
        expect(toLocalDayStartIso(null)).toBeUndefined();
        expect(toLocalDayEndIso(undefined)).toBeUndefined();
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
