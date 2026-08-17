import { describe, expect, it } from 'vitest';

import {
    clampCycleSymptom,
    normalizeCycleEndOfDay,
    normalizeCycleStartOfDay,
    toCycleDateKey,
    toNullableCycleNumber,
    toOptionalCycleText,
} from './cycle-tracking.mapper';

const MAX_SYMPTOM = 10;
const VALID_TEMPERATURE = 36.6;
const NOON_HOUR = 12;
const DAY_END_HOUR = 23;
const DAY_END_PART = 59;
const DAY_END_MILLISECONDS = 999;

describe('cycle tracking mapper', () => {
    it('clamps symptom values and handles missing or invalid input', () => {
        expect(clampCycleSymptom(-1)).toBe(0);
        expect(clampCycleSymptom(MAX_SYMPTOM + 1)).toBe(MAX_SYMPTOM);
        expect(clampCycleSymptom(Number.NaN)).toBe(0);
        expect(clampCycleSymptom(null)).toBe(0);
    });

    it('normalizes optional number and text fields', () => {
        expect(toNullableCycleNumber(String(VALID_TEMPERATURE))).toBe(VALID_TEMPERATURE);
        expect(toNullableCycleNumber('invalid')).toBeNull();
        expect(toNullableCycleNumber('')).toBeNull();
        expect(toOptionalCycleText('  note  ')).toBe('note');
        expect(toOptionalCycleText('   ')).toBeUndefined();
    });

    it('normalizes local day boundaries without mutating the source date', () => {
        const source = new Date('2026-04-12T12:30:20');
        const start = normalizeCycleStartOfDay(source);
        const end = normalizeCycleEndOfDay(source);

        expect(start.getHours()).toBe(0);
        expect(start.getMinutes()).toBe(0);
        expect(end.getHours()).toBe(DAY_END_HOUR);
        expect(end.getMinutes()).toBe(DAY_END_PART);
        expect(end.getSeconds()).toBe(DAY_END_PART);
        expect(end.getMilliseconds()).toBe(DAY_END_MILLISECONDS);
        expect(source.getHours()).toBe(NOON_HOUR);
    });

    it('builds stable ISO date keys and rejects invalid dates', () => {
        expect(toCycleDateKey('2026-04-12T22:30:00Z')).toBe('2026-04-12');
        expect(toCycleDateKey('invalid')).toBe('');
    });
});
