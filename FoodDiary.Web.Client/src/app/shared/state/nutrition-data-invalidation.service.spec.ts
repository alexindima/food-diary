import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { NutritionDataInvalidationService } from './nutrition-data-invalidation.service';

describe('NutritionDataInvalidationService', () => {
    it('invalidates every meal-dependent read model after a meal mutation', () => {
        const service = TestBed.inject(NutritionDataInvalidationService);

        service.reportMealMutation();

        expect(service.mealsVersion()).toBe(1);
        expect(service.dashboardVersion()).toBe(1);
        expect(service.statisticsVersion()).toBe(1);
    });

    it('invalidates dashboard and statistics after goal or body metric mutations', () => {
        const service = TestBed.inject(NutritionDataInvalidationService);

        service.reportGoalMutation();
        service.reportBodyMetricMutation();

        expect(service.mealsVersion()).toBe(0);
        expect(service.dashboardVersion()).toBe(2);
        expect(service.statisticsVersion()).toBe(2);
    });
});
