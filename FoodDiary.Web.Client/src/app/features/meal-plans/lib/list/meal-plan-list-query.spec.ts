import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { readMealPlanListQuery, writeMealPlanListQuery } from './meal-plan-list-query';
describe('meal plan list query', () => {
    it.each(['all', 'curated', 'saved'] as const)('round trips source %s with diet and page', source => {
        const query = { page: 3, dietType: 'Keto' as const, source };
        expect(readMealPlanListQuery(convertToParamMap(writeMealPlanListQuery(query)))).toEqual(query);
    });
    it('defaults an unknown source to all plans', () => {
        expect(readMealPlanListQuery(convertToParamMap({ source: 'unknown' })).source).toBe('all');
    });
    it('round trips selected diet and page', () => {
        expect(readMealPlanListQuery(convertToParamMap(writeMealPlanListQuery({ page: 2, dietType: 'Keto', source: 'all' })))).toEqual({
            page: 2,
            dietType: 'Keto',
            source: 'all',
        });
    });
    it.each(['0', '-1', '1.5', '10001'])('normalizes invalid page %s and unknown diet', page => {
        expect(readMealPlanListQuery(convertToParamMap({ page, dietType: 'unknown' }))).toEqual({ page: 1, dietType: null, source: 'all' });
    });
});
