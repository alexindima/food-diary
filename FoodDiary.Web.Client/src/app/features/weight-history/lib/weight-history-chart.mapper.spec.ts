import { describe, expect, it } from 'vitest';

import type { WeightEntrySummaryPoint } from '../models/weight-entry.data';
import { buildWeightEntryViewModels, buildWeightHistoryChartData } from './weight-history-chart.mapper';

const AVERAGE_WEIGHT = 72;
const POINTS: WeightEntrySummaryPoint[] = [
    { startDate: '2026-05-02T00:00:00Z', endDate: '2026-05-02T23:59:59Z', averageWeight: AVERAGE_WEIGHT },
    { startDate: '2026-05-01T00:00:00Z', endDate: '2026-05-01T23:59:59Z', averageWeight: 0 },
];

describe('weight history chart mapper', () => {
    it('sorts summary points and maps empty averages to gaps', () => {
        const data = buildWeightHistoryChartData(POINTS, 'Weight', 'en');

        expect(data.labels).toEqual(['5/1/2026', '5/2/2026']);
        expect(data.datasets[0].label).toBe('Weight');
        expect(data.datasets[0].data).toEqual([null, AVERAGE_WEIGHT]);
    });

    it('builds entry view models with localized numeric dates', () => {
        const items = buildWeightEntryViewModels([{ id: 'w-1', userId: 'u-1', date: '2026-05-15T00:00:00Z', weight: 71.5 }], 'en');

        expect(items).toEqual([
            {
                entry: { id: 'w-1', userId: 'u-1', date: '2026-05-15T00:00:00Z', weight: 71.5 },
                dateLabel: '05/15/2026',
            },
        ]);
    });
});
