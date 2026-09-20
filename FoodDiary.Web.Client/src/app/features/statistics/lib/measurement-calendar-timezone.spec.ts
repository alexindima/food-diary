import { describe, expect, it } from 'vitest';

import { formatDateInputValue, parseLocalDateInputValue } from '../../../shared/lib/local-date.utils';
import { measurementMonthRange, toMeasurementDateIso } from '../../../shared/lib/measurement-date.utils';
import { formatWaistHistoryNumericDate } from '../../waist-history/lib/waist-history-chart.mapper';
import { buildWaistHistoryFiltersForRange } from '../../waist-history/lib/waist-history-range.utils';
import { buildWeightHistoryChartPoints, formatWeightHistoryNumericDate } from '../../weight-history/lib/weight-history-chart.mapper';
import { buildWeightHistoryFiltersForRange } from '../../weight-history/lib/weight-history-range.utils';
import { buildStatisticsSummaryRequest } from './statistics-data-mapper';

// Run this suite in separate processes with TZ set: local getters must use the real host zone.
const dates = [
    '2024-02-29',
    '2026-01-01',
    '2026-03-08',
    '2026-03-29',
    '2026-04-01',
    '2026-09-21',
    '2026-10-04',
    '2026-10-25',
    '2026-11-01',
    '2026-12-31',
];

const noon = 12;
const lastHour = 23;
const hours = [0, 1, noon, lastHour];
const minute = 30;
const year = 2026;
const monthPrefixLength = 7;
const dayPrefixLength = 8;
function required<T>(value: T | null): T {
    if (value === null) {
        throw new Error('Expected a valid calendar date');
    }
    return value;
}

describe('measurement calendar dates in the host time zone', () => {
    it.each(dates)('preserves %s on create, edit, display, and report selection', value => {
        const localDate = required(parseLocalDateInputValue(value));
        for (const hour of hours) {
            const now = new Date(localDate);
            now.setHours(hour, minute);
            const formDate = formatDateInputValue(now);
            const stored = required(toMeasurementDateIso(formDate));
            expect(stored).toBe(`${value}T00:00:00.000Z`);
            expect(toMeasurementDateIso(stored.split('T')[0] ?? '')).toBe(stored);
            const expectedLabel = value.split('-').reverse().join('.');
            expect(formatWeightHistoryNumericDate(stored, 'ru')).toBe(expectedLabel);
            expect(formatWaistHistoryNumericDate(stored, 'ru')).toBe(expectedLabel);
            const request = buildStatisticsSummaryRequest({ start: now, end: now });
            expect(request.bodyDateFrom).toBe(value);
            expect(request.bodyDateTo).toBe(value);
            expect(new Date(request.dateFrom).getTime()).toBeLessThanOrEqual(now.getTime());
            expect(new Date(request.dateTo).getTime()).toBeGreaterThanOrEqual(now.getTime());
            for (const build of [buildWeightHistoryFiltersForRange, buildWaistHistoryFiltersForRange]) {
                const filters = build('custom', { start: now, end: now });
                expect(filters.summaryParams.dateFrom).toBe(stored);
                expect(filters.summaryParams.dateTo).toBe(`${value}T23:59:59.999Z`);
            }
        }
        const month = required(measurementMonthRange(value));
        expect(formatDateInputValue(month.start)).toBe(`${value.slice(0, monthPrefixLength)}-01`);
        expect(formatDateInputValue(month.end).slice(0, monthPrefixLength)).toBe(value.slice(0, monthPrefixLength));
        expect(
            buildWeightHistoryChartPoints([{ startDate: value, endDate: value, averageWeightKg: 70 }], 'ru', year)[0]?.label.split('\n')[0],
        ).toBe(value.slice(dayPrefixLength));
    });

    it.each(['', 'not-a-date', '2026-02-29', '2026-04-31', '2026-13-01'])('rejects invalid calendar date %s', value => {
        expect(toMeasurementDateIso(value)).toBeNull();
        expect(measurementMonthRange(value)).toBeNull();
    });

    it('keeps month/year boundaries and leap days in a multi-day report', () => {
        const range = { start: required(parseLocalDateInputValue('2024-02-29')), end: required(parseLocalDateInputValue('2024-03-01')) };
        expect(buildStatisticsSummaryRequest(range)).toMatchObject({ bodyDateFrom: '2024-02-29', bodyDateTo: '2024-03-01' });
        expect(formatDateInputValue(required(measurementMonthRange('2024-02-29')).end)).toBe('2024-02-29');
    });
});
