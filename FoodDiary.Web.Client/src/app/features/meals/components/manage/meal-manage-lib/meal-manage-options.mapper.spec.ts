import type { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { buildMealNutritionModeOptions, buildMealTypeSelectOptions } from './meal-manage-options.mapper';

const translateService = {
    instant: vi.fn((key: string) => key),
} as unknown as TranslateService;

describe('meal manage option mapping', () => {
    it('should build meal type and nutrition mode options', () => {
        expect(buildMealTypeSelectOptions(translateService)[0]).toEqual({
            value: 'BREAKFAST',
            label: 'MEAL_TYPES.BREAKFAST',
        });
        expect(buildMealNutritionModeOptions(translateService)).toEqual([
            {
                value: 'auto',
                label: 'CONSUMPTION_MANAGE.NUTRITION_MODE.AUTO',
            },
            {
                value: 'manual',
                label: 'CONSUMPTION_MANAGE.NUTRITION_MODE.MANUAL',
            },
        ]);
    });
});
