import { describe, expect, it } from 'vitest';

import type { WaistEntrySummaryPoint } from '../models/waist-entry.data';
import { buildWaistEntryViewModels, buildWaistHistoryChartData } from './waist-history-chart.mapper';

const AVERAGE_CIRCUMFERENCE = 82;
const POINTS: WaistEntrySummaryPoint[] = [
    { startDate: '2026-05-02T00:00:00Z', endDate: '2026-05-02T23:59:59Z', averageCircumference: AVERAGE_CIRCUMFERENCE },
    { startDate: '2026-05-01T00:00:00Z', endDate: '2026-05-01T23:59:59Z', averageCircumference: 0 },
];

describe('waist history chart mapper', () => {
    it('sorts summary points and maps empty averages to gaps', () => {
        const data = buildWaistHistoryChartData(POINTS, 'Waist', 'en');

        expect(data.labels).toEqual(['5/1/2026', '5/2/2026']);
        expect(data.datasets[0].label).toBe('Waist');
        expect(data.datasets[0].data).toEqual([null, AVERAGE_CIRCUMFERENCE]);
    });

    it('builds entry view models with localized numeric dates', () => {
        const items = buildWaistEntryViewModels([{ id: 'wa-1', userId: 'u-1', date: '2026-05-15T00:00:00Z', circumference: 81.5 }], 'en');

        expect(items).toEqual([
            {
                entry: { id: 'wa-1', userId: 'u-1', date: '2026-05-15T00:00:00Z', circumference: 81.5 },
                dateLabel: '05/15/2026',
            },
        ]);
    });
});
