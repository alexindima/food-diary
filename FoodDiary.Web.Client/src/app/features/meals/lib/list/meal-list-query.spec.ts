import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { readMealListQuery, writeMealListQuery } from './meal-list-query';

describe('meal diary URL query', () => {
    it('round trips every filter including explicit zero and false without shifting the selected days', () => {
        const values = {
            page: '2',
            dateFrom: '2024-02-29',
            dateTo: '2024-03-01',
            mealTypes: 'dinner,breakfast,dinner',
            caloriesFrom: '0',
            caloriesTo: '500',
            hasImage: 'false',
            hasAiSession: 'true',
        };
        const query = readMealListQuery(convertToParamMap(values));
        expect(query.dateRange?.start).toEqual(new Date('2024-02-29T00:00:00'));
        expect(query.dateRange?.end).toEqual(new Date('2024-03-01T00:00:00'));
        expect(writeMealListQuery(query)).toEqual({ ...values, mealTypes: 'BREAKFAST,DINNER' });
    });

    it('omits default state and keeps empty inputs distinct from zero', () => {
        expect(writeMealListQuery(readMealListQuery(convertToParamMap({ caloriesFrom: ' ', hasImage: 'unknown' })))).toEqual({
            page: null,
            dateFrom: null,
            dateTo: null,
            mealTypes: null,
            caloriesFrom: null,
            caloriesTo: null,
            hasImage: null,
            hasAiSession: null,
        });
    });

    it('rejects malformed dates, unsupported types, excessive page and nonfinite calorie values', () => {
        const query = readMealListQuery(
            convertToParamMap({
                page: '10001',
                dateFrom: '2026-02-31',
                dateTo: '2026-03-01T00:00:00Z',
                mealTypes: ['breakfast,unknown', 'breakfast'],
                caloriesFrom: '-1',
                caloriesTo: 'Infinity',
                hasAiSession: '1',
            }),
        );
        expect(query).toEqual({
            page: 1,
            dateRange: null,
            mealTypes: ['BREAKFAST'],
            caloriesFrom: null,
            caloriesTo: null,
            hasImage: null,
            hasAiSession: null,
        });
    });

    it('normalizes reversed date and calorie ranges without applying an impossible request', () => {
        const query = readMealListQuery(
            convertToParamMap({ dateFrom: '2026-04-02', dateTo: '2026-04-01', caloriesFrom: '100', caloriesTo: '0' }),
        );
        expect(query.dateRange?.start).toBeNull();
        expect(query.caloriesFrom).toBeNull();
        expect(query.caloriesTo).toBe(0);
    });
});
