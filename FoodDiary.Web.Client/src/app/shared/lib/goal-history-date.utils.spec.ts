import { describe, expect, it } from 'vitest';

import { formatGoalHistoryDates } from './goal-history-date.utils';

describe('goal history date labels', () => {
    it('collapses timestamps on the same local day', () => {
        const result = formatGoalHistoryDates('2026-08-06T10:00:00', '2026-08-06T18:00:00', 'ru');
        expect(result.dateRange).toBe('6 авг. 2026');
        expect(result.endDate).toBe(result.startDate);
    });
    it('shows the year once for a same-year interval', () => {
        expect(formatGoalHistoryDates('2026-08-06T10:00:00', '2026-09-14T10:00:00', 'ru').dateRange).toBe('6 авг. — 14 сент. 2026');
    });
    it('retains both years across a year boundary', () => {
        expect(formatGoalHistoryDates('2025-12-31T10:00:00', '2026-01-02T10:00:00', 'en').dateRange).toBe('Dec 31, 2025 — Jan 2, 2026');
    });
    it('keeps an open interval and handles invalid stored dates', () => {
        expect(formatGoalHistoryDates('2026-08-06T10:00:00', null, 'en')).toEqual({
            startDate: 'Aug 6, 2026',
            endDate: null,
            dateRange: 'Aug 6, 2026',
        });
        expect(formatGoalHistoryDates('invalid', null, 'ru').dateRange).toBe('');
        expect(formatGoalHistoryDates('2026-08-06T10:00:00', 'invalid', 'ru').dateRange).toBe('6 авг. 2026');
    });
});
