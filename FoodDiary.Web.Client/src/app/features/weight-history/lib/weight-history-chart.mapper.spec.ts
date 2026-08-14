import { describe, expect, it } from 'vitest';

import type { WeightEntrySummaryPoint } from '../models/weight-entry.data';
import { buildWeightEntryViewModels, buildWeightHistoryChartPoints } from './weight-history-chart.mapper';

const AVERAGE_WEIGHT = 72;
const CURRENT_YEAR = 2026;
const POINTS: WeightEntrySummaryPoint[] = [
    { startDate: '2026-05-02T00:00:00Z', endDate: '2026-05-02T23:59:59Z', averageWeightKg: AVERAGE_WEIGHT },
    { startDate: '2026-05-01T00:00:00Z', endDate: '2026-05-01T23:59:59Z', averageWeightKg: 0 },
];

describe('weight history chart mapper', () => {
    it('sorts summary points and maps empty averages to gaps', () => {
        const points = buildWeightHistoryChartPoints(POINTS, 'en', CURRENT_YEAR);

        expect(points).toEqual([
            { label: '01\nMay', value: null },
            { label: '02\nMay', value: AVERAGE_WEIGHT },
        ]);
    });

    it('adds the year when the range is not entirely within the current year', () => {
        const points = buildWeightHistoryChartPoints(
            [
                { startDate: '2025-12-31T00:00:00Z', endDate: '2025-12-31T23:59:59Z', averageWeightKg: AVERAGE_WEIGHT },
                { startDate: '2026-01-01T00:00:00Z', endDate: '2026-01-01T23:59:59Z', averageWeightKg: AVERAGE_WEIGHT },
            ],
            'en',
            CURRENT_YEAR,
        );

        expect(points.map(point => point.label)).toEqual(['31\nDec\n2025', '01\nJan\n2026']);
    });

    it('uses short localized month labels', () => {
        const points = buildWeightHistoryChartPoints(
            [{ startDate: '2026-07-05T00:00:00Z', endDate: '2026-07-05T23:59:59Z', averageWeightKg: AVERAGE_WEIGHT }],
            'ru',
            CURRENT_YEAR,
        );

        expect(points[0]?.label).toBe('05\nиюл.');
    });

    it('builds entry view models with localized numeric dates', () => {
        const items = buildWeightEntryViewModels([{ id: 'w-1', userId: 'u-1', date: '2026-05-15T00:00:00Z', weightKg: 71.5 }], 'en');

        expect(items).toEqual([
            {
                entry: { id: 'w-1', userId: 'u-1', date: '2026-05-15T00:00:00Z', weightKg: 71.5 },
                dateLabel: '05/15/2026',
            },
        ]);
    });
});
