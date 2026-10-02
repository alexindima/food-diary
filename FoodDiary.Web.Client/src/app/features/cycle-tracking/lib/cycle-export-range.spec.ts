import { describe, expect, it } from 'vitest';

import { cycleExportRangeError } from './cycle-export-range';

describe('cycle export range', () => {
    it.each([
        ['2025-10-01', '2026-10-02'],
        ['2024-02-29', '2025-03-01'],
        ['2026-10-02', '2026-10-02'],
    ])('accepts a valid period from %s through %s', (dateFrom, dateTo) => {
        expect(cycleExportRangeError({ dateFrom, dateTo }, '2026-10-02')).toBeNull();
    });

    it.each([
        ['2025-09-30', '2026-10-02', 'EXPORT_RANGE_TOO_LONG'],
        ['2026-10-02', '2026-10-01', 'EXPORT_DATE_ORDER'],
        ['2026-10-02', '2026-10-03', 'EXPORT_FUTURE_DATE'],
        ['', '2026-10-02', 'EXPORT_DATES_REQUIRED'],
        ['2026-02-30', '2026-10-02', 'EXPORT_DATES_REQUIRED'],
    ])('rejects invalid dates from %s through %s', (dateFrom, dateTo, error) => {
        expect(cycleExportRangeError({ dateFrom, dateTo }, '2026-10-02')).toBe(`CYCLE_TRACKING.${error}`);
    });
});
