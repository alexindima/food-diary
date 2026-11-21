import type { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { MealAiSessionManageDto } from '../../../../../shared/models/meal.data';
import {
    formatMealAiAmount,
    formatMealAiName,
    formatMealManageAmount,
    formatMealManageMacro,
    getAiSessionTotals,
} from './meal-manage-view.utils';

const FRACTION_AMOUNT = 12.5;
const KCAL_AMOUNT = 100;
const AI_GRAMS_AMOUNT = 150;
const AI_FRACTION_AMOUNT = 37.51234;

const currentLanguageMock = vi.fn(() => 'en-US');
const translateService = {
    instant: vi.fn((key: string) => key),
    getCurrentLang: currentLanguageMock,
    getFallbackLang: vi.fn(() => 'en'),
} as unknown as TranslateService;

describe('meal manage view formatting', () => {
    it('should format amount and nutrition macros using current locale', () => {
        expect(formatMealManageAmount(FRACTION_AMOUNT, 'g', translateService)).toBe('12.5 g');
        expect(formatMealManageMacro(KCAL_AMOUNT, 'GENERAL.UNITS.KCAL', translateService)).toBe('100 GENERAL.UNITS.KCAL');
    });

    it.each([
        [' GrAmS ', '150 GENERAL.UNITS.G'],
        ['l', '150 l'],
        [' Slice ', '150  Slice'],
        ['', '150'],
        ['   ', '150'],
    ])('displays recognition amount with unit %j', (unit, expected) => {
        expect(formatMealAiAmount(AI_GRAMS_AMOUNT, unit, translateService)).toBe(expected);
    });

    it.each([
        [' apple', 'Apple'],
        [' груша ', 'Груша'],
        ['   ', ''],
    ])('displays recognition item name %j', (name, expected) => {
        expect(formatMealAiName(name)).toBe(expected);
    });

    it.each([
        ['ru', '37,51234'],
        ['en', '37.51234'],
    ])('retains AI quantity precision in %s', (language, amount) => {
        currentLanguageMock.mockReturnValueOnce(language);
        expect(formatMealAiAmount(AI_FRACTION_AMOUNT, 'grams', translateService)).toBe(`${amount} GENERAL.UNITS.G`);
    });
});

describe('meal manage view totals', () => {
    it('should sum AI session nutrition totals', () => {
        const session: MealAiSessionManageDto = {
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

function createAiItem(values: {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
}): MealAiSessionManageDto['items'][number] {
    return {
        nameEn: 'Item',
        amount: 1,
        unit: 'g',
        ...values,
    };
}
