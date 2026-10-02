import { describe, expect, it } from 'vitest';

import { cycleNutritionRange } from './cycle-nutrition-range';

describe('cycle nutrition analysis range', () => {
    it.each([
        ['2026-10-02', '2026-10-02', '2026-10-02'],
        ['2026-04-01', '2026-10-02', '2026-04-01'],
        ['2025-10-02', '2026-10-02', '2025-10-02'],
        ['2025-10-01', '2026-10-02', '2025-10-02'],
        ['2025-09-30', '2026-10-02', '2025-10-02'],
        ['2020-01-01', '2024-03-01', '2023-03-02'],
        ['2020-01-01', '2026-03-29', '2025-03-29'],
    ])('uses a supported period for history starting %s through %s', (trackingStartDate, today, dateFrom) => {
        expect(cycleNutritionRange(trackingStartDate, today)).toEqual({ dateFrom, dateTo: today });
    });
});
