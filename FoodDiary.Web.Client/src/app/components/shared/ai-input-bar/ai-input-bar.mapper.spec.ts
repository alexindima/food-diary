import { describe, expect, it } from 'vitest';

import { buildMealManageDtoFromAiResult } from './ai-input-bar.mapper';

describe('buildMealManageDtoFromAiResult', () => {
    it('maps photo asset to both meal image and AI session image', () => {
        const dto = buildMealManageDtoFromAiResult(
            {
                source: 'Photo',
                mealType: 'SNACK',
                imageAssetId: 'asset-1',
                imageUrl: 'https://example.com/photo.jpg',
                recognizedAtUtc: '2026-05-02T19:00:00.000Z',
                items: [
                    {
                        nameEn: 'Tea',
                        nameLocal: 'Чай',
                        amount: 250,
                        unit: 'ml',
                        calories: 2,
                        proteins: 0,
                        fats: 0,
                        carbs: 0,
                        fiber: 0,
                        alcohol: 0,
                    },
                ],
            },
            new Date('2026-05-02T19:00:00.000Z'),
        );

        expect(dto.imageAssetId).toBe('asset-1');
        expect(dto.imageUrl).toBe('https://example.com/photo.jpg');
        expect(dto.isNutritionAutoCalculated).toBe(true);
        expect(dto.manualCalories).toBeUndefined();
        expect(dto.preMealSatietyLevel).toBe(3);
        expect(dto.postMealSatietyLevel).toBe(3);
        expect(dto.aiSessions?.[0].imageAssetId).toBe('asset-1');
        expect(dto.aiSessions?.[0].imageUrl).toBe('https://example.com/photo.jpg');
    });

    it('uses current meal date time to resolve meal type when AI bar has no preset type', () => {
        const dto = buildMealManageDtoFromAiResult(
            {
                source: 'Text',
                mealType: null,
                recognizedAtUtc: '2026-05-02T19:00:00.000Z',
                items: [],
            },
            new Date('2026-05-02T23:50:00'),
        );

        expect(dto.mealType).toBe('SNACK');
    });
});
