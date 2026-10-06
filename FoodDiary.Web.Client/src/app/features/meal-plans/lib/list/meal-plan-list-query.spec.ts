import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { readMealPlanListQuery, writeMealPlanListQuery } from './meal-plan-list-query';
describe('meal plan list query', () => {
    it('round trips selected diet and page', () => {
        expect(readMealPlanListQuery(convertToParamMap(writeMealPlanListQuery({ page: 2, dietType: 'Keto' })))).toEqual({
            page: 2,
            dietType: 'Keto',
        });
    });
    it.each(['0', '-1', '1.5', '10001'])('normalizes invalid page %s and unknown diet', page => {
        expect(readMealPlanListQuery(convertToParamMap({ page, dietType: 'unknown' }))).toEqual({ page: 1, dietType: null });
    });
});
