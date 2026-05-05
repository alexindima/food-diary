import { afterEach, describe, expect, it, vi } from 'vitest';

import { getCurrentDateRange, normalizeEndOfDay, normalizeStartOfDay } from './statistics-data-mapper';

describe('statistics-data-mapper', () => {
    afterEach(() => {
        vi.useRealTimers();
    });

    describe('getCurrentDateRange', () => {
        it('should return seven inclusive calendar days for week range', () => {
            vi.useFakeTimers();
            vi.setSystemTime(new Date(2026, 4, 6, 12, 0, 0, 0));

            const range = getCurrentDateRange('week', null);
            const start = normalizeStartOfDay(range.start);
            const end = normalizeEndOfDay(range.end);
            const days = Math.round((end.getTime() - start.getTime()) / (24 * 60 * 60 * 1000));

            expect(start).toEqual(new Date(2026, 3, 30, 0, 0, 0, 0));
            expect(end).toEqual(new Date(2026, 4, 6, 23, 59, 59, 999));
            expect(days).toBe(7);
        });
    });
});
