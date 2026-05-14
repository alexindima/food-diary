import { describe, expect, it } from 'vitest';

import type { FoodNutritionResponse } from '../models/ai.data';
import { normalizeAiNutritionItemName, recalculateEditedAiNutrition } from './ai-nutrition-edit.utils';

const SOURCE_AMOUNT = 100;
const EDITED_AMOUNT = 150;

const nutrition: FoodNutritionResponse = {
    calories: 100,
    protein: 10,
    fat: 5,
    carbs: 20,
    fiber: 2,
    alcohol: 0,
    notes: 'base',
    items: [
        {
            name: 'Apple',
            amount: SOURCE_AMOUNT,
            unit: 'g',
            calories: 100,
            protein: 10,
            fat: 5,
            carbs: 20,
            fiber: 2,
            alcohol: 0,
        },
    ],
};

describe('ai nutrition edit utils', () => {
    it('should normalize item names for matching', () => {
        expect(normalizeAiNutritionItemName(' Apple ')).toBe('apple');
        expect(normalizeAiNutritionItemName(null)).toBe('');
    });

    it('should recalculate nutrition proportionally after amount changes', () => {
        const result = recalculateEditedAiNutrition(
            nutrition,
            [{ id: '1', name: 'Apple', nameEn: 'Apple', amount: SOURCE_AMOUNT, unit: 'g' }],
            [{ id: '1', name: 'Apple', nameEn: 'Apple', amount: EDITED_AMOUNT, unit: 'g' }],
        );

        expect(result).toEqual({
            calories: 150,
            protein: 15,
            fat: 7.5,
            carbs: 30,
            fiber: 3,
            alcohol: 0,
            notes: 'base',
            items: [
                {
                    name: 'Apple',
                    amount: EDITED_AMOUNT,
                    unit: 'g',
                    calories: 150,
                    protein: 15,
                    fat: 7.5,
                    carbs: 30,
                    fiber: 3,
                    alcohol: 0,
                },
            ],
        });
    });

    it('should return null when edited item cannot be matched to original nutrition', () => {
        const result = recalculateEditedAiNutrition(
            nutrition,
            [{ id: '1', name: 'Unknown', amount: SOURCE_AMOUNT, unit: 'g' }],
            [{ id: '1', name: 'Unknown', amount: EDITED_AMOUNT, unit: 'g' }],
        );

        expect(result).toBeNull();
    });
});
