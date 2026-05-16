import { describe, expect, it } from 'vitest';

import type { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import { buildPhotoAiInputBarResult, buildTextAiInputBarResult, mapNutritionItemsToAiInputBarItems } from './ai-input-bar.mapper';
import type { AiInputBarMealDetails } from './ai-input-bar.types';

const RECOGNIZED_AT_UTC = '2026-05-16T22:00:00.000Z';
const CALORIES = 120;
const PROTEIN = 10;
const FAT = 4;
const CARBS = 12;
const FIBER = 3;
const ALCOHOL = 0;
const AMOUNT = 150;
const CONFIDENCE = 0.9;
const SATIETY_BEFORE = 3;
const SATIETY_AFTER = 7;

const DETAILS: AiInputBarMealDetails = {
    date: '2026-05-16',
    time: '12:30',
    comment: 'Lunch',
    preMealSatietyLevel: SATIETY_BEFORE,
    postMealSatietyLevel: SATIETY_AFTER,
};

describe('ai input bar mapper', () => {
    it('maps nutrition items to result items using local name match', () => {
        const [item] = mapNutritionItemsToAiInputBarItems(createNutrition({ itemName: 'яблоко' }), [createVisionItem()]);

        expect(item).toEqual({
            nameEn: 'Apple',
            nameLocal: 'яблоко',
            amount: AMOUNT,
            unit: 'g',
            calories: CALORIES,
            proteins: PROTEIN,
            fats: FAT,
            carbs: CARBS,
            fiber: FIBER,
            alcohol: ALCOHOL,
        });
    });

    it('builds text result with meal details and query notes', () => {
        const result = buildTextAiInputBarResult({
            source: 'Voice',
            mealType: 'Lunch',
            recognizedAtUtc: RECOGNIZED_AT_UTC,
            query: 'apple',
            details: DETAILS,
            nutrition: createNutrition(),
            results: [createVisionItem()],
        });

        expect(result.source).toBe('Voice');
        expect(result.notes).toBe('apple');
        expect(result.date).toBe(DETAILS.date);
        expect(result.preMealSatietyLevel).toBe(SATIETY_BEFORE);
        expect(result.items).toHaveLength(1);
    });

    it('builds photo result with selected image and nutrition notes', () => {
        const result = buildPhotoAiInputBarResult({
            mealType: 'Dinner',
            recognizedAtUtc: RECOGNIZED_AT_UTC,
            selection: { assetId: 'asset-1', url: 'https://example.test/image.jpg' },
            details: DETAILS,
            nutrition: createNutrition({ notes: 'Looks like fruit' }),
            results: [createVisionItem()],
        });

        expect(result.source).toBe('Photo');
        expect(result.imageAssetId).toBe('asset-1');
        expect(result.imageUrl).toBe('https://example.test/image.jpg');
        expect(result.notes).toBe('Looks like fruit');
        expect(result.items[0].nameEn).toBe('Apple');
    });
});

function createVisionItem(): FoodVisionItem {
    return {
        nameEn: 'Apple',
        nameLocal: 'яблоко',
        amount: AMOUNT,
        unit: 'g',
        confidence: CONFIDENCE,
    };
}

function createNutrition(overrides: { itemName?: string; notes?: string | null } = {}): FoodNutritionResponse {
    return {
        calories: CALORIES,
        protein: PROTEIN,
        fat: FAT,
        carbs: CARBS,
        fiber: FIBER,
        alcohol: ALCOHOL,
        notes: overrides.notes ?? null,
        items: [
            {
                name: overrides.itemName ?? 'Apple',
                amount: AMOUNT,
                unit: 'g',
                calories: CALORIES,
                protein: PROTEIN,
                fat: FAT,
                carbs: CARBS,
                fiber: FIBER,
                alcohol: ALCOHOL,
            },
        ],
    };
}
