import { HttpStatusCode } from '@angular/common/http';
import { describe, expect, it } from 'vitest';

import type { FoodNutritionResponse, FoodVisionItem } from '../../../../../shared/models/ai.data';
import { MeasurementUnit } from '../../../models/product.data';
import {
    buildProductAiRecognitionModelFromNutrition,
    buildProductAiRecognitionResult,
    createProductAiRecognitionFormModel,
    getRecognizedAmount,
    isProductAiRecognitionModelValid,
    mapAiNutritionErrorKey,
    mapAiRecognitionErrorKey,
    normalizeItemsForNutrition,
    resolveAiMeasurementUnit,
} from './product-ai-recognition.helpers';

const HALF_LITER_ML = 500;
const RECOGNIZED_GRAMS = 120;
const RECOGNIZED_MILLILITERS = 200;
const DEFAULT_GRAMS = 100;

const ITEMS: FoodVisionItem[] = [
    {
        nameEn: 'apple',
        nameLocal: 'apple local',
        amount: RECOGNIZED_GRAMS,
        unit: 'g',
        confidence: 0.9,
    },
    {
        nameEn: 'milk',
        amount: RECOGNIZED_MILLILITERS,
        unit: 'ml',
        confidence: 0.8,
    },
];

const NUTRITION: FoodNutritionResponse = {
    calories: 150,
    protein: 4,
    fat: 2,
    carbs: 25,
    fiber: 3,
    alcohol: 0,
    items: [],
};

describe('product AI recognition helpers', () => {
    it('should resolve common AI units', () => {
        expect(resolveAiMeasurementUnit('grams')).toBe(MeasurementUnit.G);
        expect(resolveAiMeasurementUnit('liter')).toBe(MeasurementUnit.ML);
        expect(resolveAiMeasurementUnit('piece')).toBe(MeasurementUnit.PCS);
        expect(resolveAiMeasurementUnit('unknown')).toBe(MeasurementUnit.G);
    });

    it('should normalize items for nutrition request', () => {
        const normalized = normalizeItemsForNutrition([
            ...ITEMS,
            {
                nameEn: 'egg',
                amount: 0,
                unit: 'piece',
                confidence: 0.7,
            },
        ]);

        expect(normalized[0].unit).toBe('g');
        expect(normalized[1].unit).toBe('ml');
        expect(normalized[2].unit).toBe('pcs');
        expect(normalized[2].amount).toBe(1);
    });

    it('should calculate recognized amount for compatible units', () => {
        expect(getRecognizedAmount(ITEMS, MeasurementUnit.G)).toBe(RECOGNIZED_GRAMS);
        expect(getRecognizedAmount(ITEMS, MeasurementUnit.ML)).toBe(RECOGNIZED_MILLILITERS);
        expect(getRecognizedAmount([], MeasurementUnit.PCS)).toBe(1);
    });

    it('should apply nutrition response to result form', () => {
        const model = buildProductAiRecognitionModelFromNutrition(ITEMS, NUTRITION);

        expect(model.name).toBe('Apple local');
        expect(model.portionAmount).toBe(RECOGNIZED_GRAMS);
        expect(model.baseUnit).toBe(MeasurementUnit.G);
        expect(model.caloriesPerBase).toBe(NUTRITION.calories);
    });

    it('should build dialog result with fallback name and copied image', () => {
        const model = createProductAiRecognitionFormModel();
        const image = { assetId: 'asset-1', url: 'https://example.test/image.jpg' };

        const result = buildProductAiRecognitionResult({
            model,
            selection: image,
            itemNames: ['Fallback name'],
            results: ITEMS,
            description: 'fresh',
        });

        expect(result.name).toBe('Fallback name');
        expect(result.description).toBe('fresh');
        expect(result.image).toEqual(image);
        expect(result.image).not.toBe(image);
        expect(result.baseAmount).toBe(DEFAULT_GRAMS);
    });

    it('should map API errors to translation keys', () => {
        expect(mapAiRecognitionErrorKey({ status: HttpStatusCode.Forbidden })).toBe('PRODUCT_AI_DIALOG.ERROR_PREMIUM');
        expect(mapAiRecognitionErrorKey({ status: HttpStatusCode.TooManyRequests })).toBe('PRODUCT_AI_DIALOG.ERROR_QUOTA');
        expect(mapAiRecognitionErrorKey({ status: HttpStatusCode.InternalServerError })).toBe('PRODUCT_AI_DIALOG.ERROR_GENERIC');
        expect(mapAiNutritionErrorKey({ status: HttpStatusCode.TooManyRequests })).toBe('PRODUCT_AI_DIALOG.ERROR_QUOTA');
        expect(mapAiNutritionErrorKey({ status: HttpStatusCode.InternalServerError })).toBe('PRODUCT_AI_DIALOG.NUTRITION_ERROR');
    });
});

describe('product recognition quantities and review validation', () => {
    it('converts liters to milliliters consistently for provider input and reviewed quantity', () => {
        const items = [{ ...ITEMS[1], amount: 0.5, unit: 'liter' }];
        expect(normalizeItemsForNutrition(items)[0]).toEqual(expect.objectContaining({ amount: HALF_LITER_ML, unit: 'ml' }));
        expect(getRecognizedAmount(items, MeasurementUnit.ML)).toBe(HALF_LITER_ML);
        expect(buildProductAiRecognitionModelFromNutrition(items, NUTRITION).portionAmount).toBe(HALF_LITER_ML);
    });

    it('permits genuine zero nutrients but rejects missing values rather than replacing them with zero', () => {
        const model = { ...createProductAiRecognitionFormModel(), name: 'Water' };
        expect(isProductAiRecognitionModelValid(model)).toBe(true);
        const missing = buildProductAiRecognitionModelFromNutrition(ITEMS, {
            ...NUTRITION,
            calories: null,
        } as unknown as FoodNutritionResponse);
        expect(missing.caloriesPerBase).toBeNull();
        expect(isProductAiRecognitionModelValid(missing)).toBe(false);
    });

    it.each([0, -1, NaN, Infinity])('rejects invalid quantity %s', portionAmount => {
        expect(isProductAiRecognitionModelValid({ ...createProductAiRecognitionFormModel(), name: 'Water', portionAmount })).toBe(false);
    });
});
