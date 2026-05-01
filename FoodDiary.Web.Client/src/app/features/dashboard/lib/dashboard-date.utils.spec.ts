import { describe, expect, it } from 'vitest';

import {
    getDashboardDateUtc,
    getHydrationDateUtc,
    getWeightTrendRange,
    normalizeDate,
    normalizeEndOfDayUtc,
    normalizeStartOfDayUtc,
} from './dashboard-date.utils';

describe('dashboard-date.utils', () => {
    describe('normalizeDate', () => {
        it('should zero out hours, minutes, seconds, milliseconds', () => {
            const input = new Date(2026, 2, 15, 14, 30, 45, 123);
            const result = normalizeDate(input);
            expect(result.getHours()).toBe(0);
            expect(result.getMinutes()).toBe(0);
            expect(result.getSeconds()).toBe(0);
            expect(result.getMilliseconds()).toBe(0);
        });

        it('should preserve the date part', () => {
            const input = new Date(2026, 5, 20, 18, 0, 0);
            const result = normalizeDate(input);
            expect(result.getFullYear()).toBe(2026);
            expect(result.getMonth()).toBe(5);
            expect(result.getDate()).toBe(20);
        });

        it('should not mutate the original date', () => {
            const input = new Date(2026, 0, 1, 12, 0, 0);
            normalizeDate(input);
            expect(input.getHours()).toBe(12);
        });
    });

    describe('getDashboardDateUtc', () => {
        it('should return a UTC date at midnight', () => {
            const input = new Date(2026, 3, 10);
            const result = getDashboardDateUtc(input);
            expect(result.getUTCHours()).toBe(0);
            expect(result.getUTCMinutes()).toBe(0);
            expect(result.getUTCSeconds()).toBe(0);
            expect(result.getUTCFullYear()).toBe(2026);
            expect(result.getUTCMonth()).toBe(3);
            expect(result.getUTCDate()).toBe(10);
        });
    });

    describe('getHydrationDateUtc', () => {
        it('should return a UTC date at noon', () => {
            const input = new Date(2026, 6, 5);
            const result = getHydrationDateUtc(input);
            expect(result.getUTCHours()).toBe(12);
            expect(result.getUTCMinutes()).toBe(0);
            expect(result.getUTCFullYear()).toBe(2026);
            expect(result.getUTCMonth()).toBe(6);
            expect(result.getUTCDate()).toBe(5);
        });
    });

    describe('normalizeStartOfDayUtc', () => {
        it('should return UTC start of day', () => {
            const input = new Date(2026, 11, 25);
            const result = normalizeStartOfDayUtc(input);
            expect(result.getUTCHours()).toBe(0);
            expect(result.getUTCMinutes()).toBe(0);
            expect(result.getUTCSeconds()).toBe(0);
            expect(result.getUTCMilliseconds()).toBe(0);
        });
    });

    describe('normalizeEndOfDayUtc', () => {
        it('should return UTC end of day', () => {
            const input = new Date(2026, 11, 25);
            const result = normalizeEndOfDayUtc(input);
            expect(result.getUTCHours()).toBe(23);
            expect(result.getUTCMinutes()).toBe(59);
            expect(result.getUTCSeconds()).toBe(59);
            expect(result.getUTCMilliseconds()).toBe(999);
        });
    });

    describe('getWeightTrendRange', () => {
        it('should return a range of trendDays days ending on selectedDate', () => {
            const selectedDate = new Date(2026, 2, 15);
            const { start, end } = getWeightTrendRange(selectedDate, 7);

            expect(start.getUTCDate()).toBe(9);
            expect(start.getUTCMonth()).toBe(2);
            expect(end.getUTCDate()).toBe(15);
            expect(end.getUTCMonth()).toBe(2);
        });

        it('should handle month boundary', () => {
            const selectedDate = new Date(2026, 3, 3);
            const { start } = getWeightTrendRange(selectedDate, 7);
            expect(start.getUTCMonth()).toBe(2);
            expect(start.getUTCDate()).toBe(28);
        });

        it('should work with trendDays = 1', () => {
            const selectedDate = new Date(2026, 0, 10);
            const { start, end } = getWeightTrendRange(selectedDate, 1);
            expect(start.getUTCDate()).toBe(10);
            expect(end.getUTCDate()).toBe(10);
        });
    });
});
