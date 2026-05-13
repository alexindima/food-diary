import { HttpStatusCode } from '@angular/common/http';
import { describe, expect, it } from 'vitest';

import type { FoodNutritionResponse, FoodVisionItem } from '../../../../../shared/models/ai.data';
import { MeasurementUnit } from '../../../models/product.data';
import {
    applyNutritionToProductAiRecognitionForm,
    buildProductAiRecognitionResult,
    createProductAiRecognitionForm,
    getRecognizedAmount,
    mapAiNutritionErrorKey,
    mapAiRecognitionErrorKey,
    normalizeItemsForNutrition,
    resolveAiMeasurementUnit,
} from './product-ai-recognition.helpers';

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
        const form = createProductAiRecognitionForm();

        applyNutritionToProductAiRecognitionForm(form, ITEMS, NUTRITION);

        expect(form.controls.name.value).toBe('Apple local');
        expect(form.controls.portionAmount.value).toBe(RECOGNIZED_GRAMS);
        expect(form.controls.baseUnit.value).toBe(MeasurementUnit.G);
        expect(form.controls.caloriesPerBase.value).toBe(NUTRITION.calories);
    });

    it('should build dialog result with fallback name and copied image', () => {
        const form = createProductAiRecognitionForm();
        const image = { assetId: 'asset-1', url: 'https://example.test/image.jpg' };

        const result = buildProductAiRecognitionResult({
            form,
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
