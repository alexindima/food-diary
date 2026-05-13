import { FormControl, Validators } from '@angular/forms';
import type { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { ConsumptionAiSessionManageDto } from '../../models/meal.data';
import {
    formatMealAiAmount,
    formatMealAiName,
    formatMealManageAmount,
    formatMealManageMacro,
    getAiSessionTotals,
    resolveMealManageControlError,
} from './meal-manage-view.utils';

const FRACTION_AMOUNT = 12.5;
const KCAL_AMOUNT = 100;
const AI_GRAMS_AMOUNT = 150;

const translateService = {
    instant: vi.fn((key: string, params?: Record<string, unknown>) => {
        const min = params?.['min'];
        return typeof min === 'number' ? `${key}:${min}` : key;
    }),
    getCurrentLang: vi.fn(() => 'en-US'),
    getFallbackLang: vi.fn(() => 'en'),
} as unknown as TranslateService;

describe('meal manage view formatting', () => {
    it('should format amount and nutrition macros using current locale', () => {
        expect(formatMealManageAmount(FRACTION_AMOUNT, 'g', translateService)).toBe('12.5 g');
        expect(formatMealManageMacro(KCAL_AMOUNT, 'GENERAL.UNITS.KCAL', translateService)).toBe('100 GENERAL.UNITS.KCAL');
    });

    it('should normalize AI amount unit and food name labels', () => {
        expect(formatMealAiAmount(AI_GRAMS_AMOUNT, 'grams', translateService)).toBe('150 GENERAL.UNITS.G');
        expect(formatMealAiAmount(2, 'slice', translateService)).toBe('2 slice');
        expect(formatMealAiName(' apple')).toBe('Apple');
        expect(formatMealAiName('   ')).toBe('');
    });
});

describe('meal manage view totals', () => {
    it('should sum AI session nutrition totals', () => {
        const session: ConsumptionAiSessionManageDto = {
            items: [
                createAiItem({ calories: 10, proteins: 2, fats: 1, carbs: 3, fiber: 4, alcohol: 0 }),
                createAiItem({ calories: 20, proteins: 3, fats: 2, carbs: 4, fiber: 5, alcohol: 1 }),
            ],
        };

        expect(getAiSessionTotals(session)).toEqual({
            calories: 30,
            proteins: 5,
            fats: 3,
            carbs: 7,
            fiber: 9,
            alcohol: 1,
        });
    });
});

describe('meal manage control errors', () => {
    it('should resolve required and min errors only after touch', () => {
        const requiredControl = new FormControl<string | null>(null, Validators.required);

        expect(resolveMealManageControlError(requiredControl, translateService)).toBeNull();

        requiredControl.markAsTouched();
        requiredControl.updateValueAndValidity();

        expect(resolveMealManageControlError(requiredControl, translateService)).toBe('FORM_ERRORS.REQUIRED');

        const minControl = new FormControl(0, Validators.min(1));
        minControl.markAsTouched();
        minControl.updateValueAndValidity();

        expect(resolveMealManageControlError(minControl, translateService)).toBe('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO:1');
    });
});

function createAiItem(values: {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
}): ConsumptionAiSessionManageDto['items'][number] {
    return {
        nameEn: 'Item',
        amount: 1,
        unit: 'g',
        ...values,
    };
}
