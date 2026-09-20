import { describe, expect, it } from 'vitest';

import type { WaistEntrySummaryPoint } from '../models/waist-entry.data';
import { buildWaistEntryViewModels, buildWaistHistoryChartPoints } from './waist-history-chart.mapper';

const CURRENT_YEAR = 2026;
const AVERAGE_CIRCUMFERENCE = 82;
const POINTS: WaistEntrySummaryPoint[] = [
    { startDate: '2026-05-02T00:00:00Z', endDate: '2026-05-02T23:59:59Z', averageCircumferenceCm: AVERAGE_CIRCUMFERENCE },
    { startDate: '2026-05-01T00:00:00Z', endDate: '2026-05-01T23:59:59Z', averageCircumferenceCm: 0 },
];

describe('waist history chart mapper', () => {
    it('sorts summary points and maps empty averages to gaps', () => {
        const points = buildWaistHistoryChartPoints(POINTS, 'en', CURRENT_YEAR);

        expect(points).toEqual([
            { label: '01\nMay', value: null },
            { label: '02\nMay', value: AVERAGE_CIRCUMFERENCE },
        ]);
    });

    it('builds entry view models with localized numeric dates', () => {
        const items = buildWaistEntryViewModels([{ id: 'wa-1', userId: 'u-1', date: '2026-05-15T00:00:00Z', circumferenceCm: 81.5 }], 'en');

        expect(items).toEqual([
            {
                entry: { id: 'wa-1', userId: 'u-1', date: '2026-05-15T00:00:00Z', circumferenceCm: 81.5 },
                dateLabel: '05/15/2026',
            },
        ]);
    });
});

describe('Chart date boundaries', () => {
    it('keeps malformed dates visible rather than crashing and preserves gaps', () => {
        const entry = { id: 'invalid', userId: 'u', date: 'invalid', circumferenceCm: 80 };
        expect(buildWaistEntryViewModels([entry], 'ru')[0].dateLabel).toBe('invalid');
        expect(buildWaistHistoryChartPoints([{ startDate: 'invalid', endDate: 'invalid', averageCircumferenceCm: 0 }], 'ru')).toEqual([
            { label: 'invalid', value: null },
        ]);
        expect(buildWaistHistoryChartPoints([], 'ru')).toEqual([]);
    });
    it.each([
        { locale: 'en', date: '2025-09-01', expected: '01\nSep\n2025' },
        { locale: 'ru', date: '2025-09-01', expected: '01\nсент.\n2025' },
        { locale: 'en', date: '2025-05-01', expected: '01\nMay\n2025' },
    ])('labels $locale dates from earlier years', ({ locale, date, expected }) => {
        expect(
            buildWaistHistoryChartPoints([{ startDate: date, endDate: date, averageCircumferenceCm: 80 }], locale, CURRENT_YEAR)[0].label,
        ).toBe(expected);
    });
});
