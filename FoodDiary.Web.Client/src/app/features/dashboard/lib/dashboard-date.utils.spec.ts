import { describe, expect, it } from 'vitest';

import {
    getDashboardDateUtc,
    getHydrationDateUtc,
    getWeightTrendRange,
    normalizeDate,
    normalizeEndOfDayUtc,
    normalizeStartOfDayUtc,
} from './dashboard-date.utils';

const YEAR = 2026;
const JANUARY = 0;
const MARCH = 2;
const APRIL = 3;
const JUNE = 5;
const JULY = 6;
const DECEMBER = 11;
const DAY_3 = 3;
const DAY_5 = 5;
const DAY_9 = 9;
const DAY_10 = 10;
const DAY_15 = 15;
const DAY_20 = 20;
const DAY_25 = 25;
const DAY_28 = 28;
const NOON_HOUR = 12;
const AFTERNOON_HOUR = 14;
const EVENING_HOUR = 18;
const HALF_HOUR_MINUTES = 30;
const SECONDS_45 = 45;
const MS_123 = 123;
const END_OF_DAY_HOURS = 23;
const END_OF_DAY_MINUTES = 59;
const END_OF_DAY_SECONDS = 59;
const END_OF_DAY_MS = 999;
const WEEK_TREND_DAYS = 7;

describe('dashboard-date.utils', () => {
    registerNormalizeDateTests();
    registerDashboardDateTests();
    registerHydrationDateTests();
    registerUtcBoundaryTests();
    registerWeightTrendRangeTests();
});

function registerNormalizeDateTests(): void {
    describe('normalizeDate', () => {
        it('should zero out hours, minutes, seconds, milliseconds', () => {
            const input = new Date(YEAR, MARCH, DAY_15, AFTERNOON_HOUR, HALF_HOUR_MINUTES, SECONDS_45, MS_123);
            const result = normalizeDate(input);
            expect(result.getHours()).toBe(0);
            expect(result.getMinutes()).toBe(0);
            expect(result.getSeconds()).toBe(0);
            expect(result.getMilliseconds()).toBe(0);
        });

        it('should preserve the date part', () => {
            const input = new Date(YEAR, JUNE, DAY_20, EVENING_HOUR, 0, 0);
            const result = normalizeDate(input);
            expect(result.getFullYear()).toBe(YEAR);
            expect(result.getMonth()).toBe(JUNE);
            expect(result.getDate()).toBe(DAY_20);
        });

        it('should not mutate the original date', () => {
            const input = new Date(YEAR, JANUARY, 1, NOON_HOUR, 0, 0);
            normalizeDate(input);
            expect(input.getHours()).toBe(NOON_HOUR);
        });
    });
}

function registerDashboardDateTests(): void {
    describe('getDashboardDateUtc', () => {
        it('should return a UTC date at midnight', () => {
            const input = new Date(YEAR, APRIL, DAY_10);
            const result = getDashboardDateUtc(input);
            expect(result.getUTCHours()).toBe(0);
            expect(result.getUTCMinutes()).toBe(0);
            expect(result.getUTCSeconds()).toBe(0);
            expect(result.getUTCFullYear()).toBe(YEAR);
            expect(result.getUTCMonth()).toBe(APRIL);
            expect(result.getUTCDate()).toBe(DAY_10);
        });
    });
}

function registerHydrationDateTests(): void {
    describe('getHydrationDateUtc', () => {
        it('should return a UTC date at noon', () => {
            const input = new Date(YEAR, JULY, DAY_5);
            const result = getHydrationDateUtc(input);
            expect(result.getUTCHours()).toBe(NOON_HOUR);
            expect(result.getUTCMinutes()).toBe(0);
            expect(result.getUTCFullYear()).toBe(YEAR);
            expect(result.getUTCMonth()).toBe(JULY);
            expect(result.getUTCDate()).toBe(DAY_5);
        });
    });
}

function registerUtcBoundaryTests(): void {
    describe('normalizeStartOfDayUtc', () => {
        it('should return UTC start of day', () => {
            const input = new Date(YEAR, DECEMBER, DAY_25);
            const result = normalizeStartOfDayUtc(input);
            expect(result.getUTCHours()).toBe(0);
            expect(result.getUTCMinutes()).toBe(0);
            expect(result.getUTCSeconds()).toBe(0);
            expect(result.getUTCMilliseconds()).toBe(0);
        });
    });

    describe('normalizeEndOfDayUtc', () => {
        it('should return UTC end of day', () => {
            const input = new Date(YEAR, DECEMBER, DAY_25);
            const result = normalizeEndOfDayUtc(input);
            expect(result.getUTCHours()).toBe(END_OF_DAY_HOURS);
            expect(result.getUTCMinutes()).toBe(END_OF_DAY_MINUTES);
            expect(result.getUTCSeconds()).toBe(END_OF_DAY_SECONDS);
            expect(result.getUTCMilliseconds()).toBe(END_OF_DAY_MS);
        });
    });
}

function registerWeightTrendRangeTests(): void {
    describe('getWeightTrendRange', () => {
        it('should return a range of trendDays days ending on selectedDate', () => {
            const selectedDate = new Date(YEAR, MARCH, DAY_15);
            const { start, end } = getWeightTrendRange(selectedDate, WEEK_TREND_DAYS);

            expect(start.getUTCDate()).toBe(DAY_9);
            expect(start.getUTCMonth()).toBe(MARCH);
            expect(end.getUTCDate()).toBe(DAY_15);
            expect(end.getUTCMonth()).toBe(MARCH);
        });

        it('should handle month boundary', () => {
            const selectedDate = new Date(YEAR, APRIL, DAY_3);
            const { start } = getWeightTrendRange(selectedDate, WEEK_TREND_DAYS);
            expect(start.getUTCMonth()).toBe(MARCH);
            expect(start.getUTCDate()).toBe(DAY_28);
        });

        it('should work with trendDays = 1', () => {
            const selectedDate = new Date(YEAR, JANUARY, DAY_10);
            const { start, end } = getWeightTrendRange(selectedDate, 1);
            expect(start.getUTCDate()).toBe(DAY_10);
            expect(end.getUTCDate()).toBe(DAY_10);
        });
    });
}
