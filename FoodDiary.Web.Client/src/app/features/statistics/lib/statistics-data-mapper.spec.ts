import { afterEach, describe, expect, it, vi } from 'vitest';

import { getCurrentDateRange, normalizeEndOfDay, normalizeStartOfDay } from './statistics-data-mapper';

const TEST_YEAR = 2026;
const MAY_INDEX = 4;
const APRIL_INDEX = 3;
const CURRENT_DAY = 6;
const NOON_HOUR = 12;
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
    });
});
