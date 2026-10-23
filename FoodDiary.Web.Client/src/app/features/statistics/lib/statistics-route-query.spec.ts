import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { readStatisticsRouteQuery, writeStatisticsRouteQuery } from './statistics-route-query';

describe('Statistics report URL query', () => {
    it('round trips local calendar dates and the nutrition tab without turning them into UTC instants', () => {
        const input = { range: 'custom', dateFrom: '2024-02-29', dateTo: '2024-03-10', nutritionTab: 'macros' };
        const query = readStatisticsRouteQuery(convertToParamMap(input));
        expect(query.customRange?.start).toEqual(new Date('2024-02-29T00:00:00'));
        expect(query.customRange?.start.getHours()).toBe(0);
        expect(writeStatisticsRouteQuery(query)).toEqual(input);
    });

    it('keeps relative presets relative and omits default filters', () => {
        const query = readStatisticsRouteQuery(convertToParamMap({ range: 'year', dateFrom: '2025-01-01', dateTo: '2025-01-25' }));
        expect(query.customRange).toBeNull();
        expect(writeStatisticsRouteQuery(query)).toEqual({ range: 'year', nutritionTab: null, dateFrom: null, dateTo: null });
        expect(writeStatisticsRouteQuery(readStatisticsRouteQuery(convertToParamMap({})))).toEqual({
            range: null,
            nutritionTab: null,
            dateFrom: null,
            dateTo: null,
        });
    });

    it.each(['2025-02-29', '2025-13-01', '2025-01-01T00:00:00Z', 'invalid', ''])('rejects malformed calendar date %s', dateFrom => {
        expect(
            readStatisticsRouteQuery(convertToParamMap({ range: 'custom', dateFrom, dateTo: '2025-03-01', nutritionTab: 'unknown' })),
        ).toEqual({
            range: 'week',
            nutritionTab: 'calories',
            customRange: null,
        });
    });

    it('orders reversed dates consistently with the report range mapper', () => {
        const query = readStatisticsRouteQuery(convertToParamMap({ range: 'custom', dateFrom: '2025-01-25', dateTo: '2025-01-01' }));
        expect(query.customRange?.start).toEqual(new Date('2025-01-01T00:00:00'));
        expect(writeStatisticsRouteQuery(query)).toEqual({
            range: 'custom',
            dateFrom: '2025-01-01',
            dateTo: '2025-01-25',
            nutritionTab: null,
        });
    });
});
