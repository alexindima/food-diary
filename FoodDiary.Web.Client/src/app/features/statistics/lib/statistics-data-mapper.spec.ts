import { afterEach, describe, expect, it, vi } from 'vitest';

import {
    buildBodyChartPoints,
    getCurrentDateRange,
    getDateRangeDayCount,
    normalizeEndOfDay,
    normalizeStartOfDay,
} from './statistics-data-mapper';

const TEST_YEAR = 2026;
const MAY_INDEX = 4;
const APRIL_INDEX = 3;
const FEBRUARY_INDEX = 1;
const NOVEMBER_INDEX = 10;
const CURRENT_DAY = 6;
const NOON_HOUR = 12;
const THIRD_DAY = 3;
const WEEK_START_DAY = 30;
const END_OF_DAY_HOUR = 23;
const END_OF_DAY_MINUTE = 59;
const END_OF_DAY_SECOND = 59;
const END_OF_DAY_MS = 999;
const HOURS_PER_DAY = 24;
const MINUTES_PER_HOUR = 60;
const SECONDS_PER_MINUTE = 60;
const MS_PER_SECOND = 1000;
const EXPECTED_WEEK_DAYS = 7;
const EXPECTED_THREE_DAYS = 3;
const BODY_START_VALUE = 80;
const BODY_END_VALUE = 82;
const BODY_INTERPOLATED_VALUE = 81;
const MS_PER_DAY = HOURS_PER_DAY * MINUTES_PER_HOUR * SECONDS_PER_MINUTE * MS_PER_SECOND;

describe('statistics-data-mapper', () => {
    afterEach(() => {
        vi.useRealTimers();
    });

    describe('getCurrentDateRange', () => {
        it('should return seven inclusive calendar days for week range', () => {
            vi.useFakeTimers();
            vi.setSystemTime(new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY, NOON_HOUR, 0, 0, 0));

            const range = getCurrentDateRange('week', null);
            const start = normalizeStartOfDay(range.start);
            const end = normalizeEndOfDay(range.end);
            const days = Math.round((end.getTime() - start.getTime()) / MS_PER_DAY);

            expect(start).toEqual(new Date(TEST_YEAR, APRIL_INDEX, WEEK_START_DAY, 0, 0, 0, 0));
            expect(end).toEqual(
                new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY, END_OF_DAY_HOUR, END_OF_DAY_MINUTE, END_OF_DAY_SECOND, END_OF_DAY_MS),
            );
            expect(days).toBe(EXPECTED_WEEK_DAYS);
        });

        it('should normalize reversed custom range boundaries', () => {
            const start = new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY);
            const end = new Date(TEST_YEAR, MAY_INDEX, THIRD_DAY);

            const range = getCurrentDateRange('custom', { start, end });

            expect(range.start).toBe(end);
            expect(range.end).toBe(start);
        });

        it('should return three months for quarter range', () => {
            vi.useFakeTimers();
            vi.setSystemTime(new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY, NOON_HOUR, 0, 0, 0));

            const range = getCurrentDateRange('quarter', null);

            expect(range.start).toEqual(new Date(TEST_YEAR, FEBRUARY_INDEX, CURRENT_DAY, NOON_HOUR, 0, 0, 0));
            expect(range.end).toEqual(new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY, NOON_HOUR, 0, 0, 0));
        });

        it('should return six months for half-year range', () => {
            vi.useFakeTimers();
            vi.setSystemTime(new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY, NOON_HOUR, 0, 0, 0));

            const range = getCurrentDateRange('halfYear', null);

            expect(range.start).toEqual(new Date(TEST_YEAR - 1, NOVEMBER_INDEX, CURRENT_DAY, NOON_HOUR, 0, 0, 0));
            expect(range.end).toEqual(new Date(TEST_YEAR, MAY_INDEX, CURRENT_DAY, NOON_HOUR, 0, 0, 0));
        });
    });

    it('counts inclusive calendar days in a date range', () => {
        const dayCount = getDateRangeDayCount({
            start: new Date(TEST_YEAR, MAY_INDEX, 1, NOON_HOUR),
            end: new Date(TEST_YEAR, MAY_INDEX, THIRD_DAY, NOON_HOUR),
        });

        expect(dayCount).toBe(EXPECTED_THREE_DAYS);
    });

    it('builds body chart points with interpolated missing values', () => {
        const points = buildBodyChartPoints(
            [
                { startDate: '2026-05-01', value: BODY_START_VALUE },
                { startDate: '2026-05-02', value: 0 },
                { startDate: '2026-05-03', value: BODY_END_VALUE },
            ],
            point => point.value,
            date => date,
        );

        expect(points).toEqual([
            { label: '2026-05-01', value: BODY_START_VALUE },
            { label: '2026-05-02', value: BODY_INTERPOLATED_VALUE },
            { label: '2026-05-03', value: BODY_END_VALUE },
        ]);
    });
});
