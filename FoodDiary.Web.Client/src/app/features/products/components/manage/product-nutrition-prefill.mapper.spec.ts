import { describe, expect, it } from 'vitest';

import { KJ_TO_KCAL_FACTOR } from '../../../../shared/lib/nutrition.constants';
import { USDA_NUTRIENT_IDS } from '../../../usda/lib/usda-nutrient.constants';
import type { UsdaFoodDetail } from '../../../usda/models/usda.data';
import type { OpenFoodFactsProduct } from '../../api/open-food-facts.service';
import { MeasurementUnit, type ProductSearchSuggestion, ProductType, ProductVisibility } from '../../models/product.data';
import type { ProductFormValues } from './product-manage-form.types';
import {
    buildOpenFoodFactsLookupPatch,
    buildResetNutritionPatch,
    buildSourceProductPrefillPatch,
    buildUsdaFoodDetailPrefillPatch,
} from './product-nutrition-prefill.mapper';

const DEFAULT_BASE_AMOUNT = 100;
const EMPTY_FORM_VALUES: ProductFormValues = {
    name: '',
    barcode: null,
    brand: null,
    productType: ProductType.Unknown,
    description: null,
    comment: null,
    imageUrl: null,
    baseAmount: DEFAULT_BASE_AMOUNT,
    defaultPortionAmount: DEFAULT_BASE_AMOUNT,
    baseUnit: MeasurementUnit.G,
    caloriesPerBase: null,
    proteinsPerBase: null,
    fatsPerBase: null,
    carbsPerBase: null,
    fiberPerBase: null,
    alcoholPerBase: null,
    visibility: ProductVisibility.Private,
    usdaFdcId: null,
};

const OFF_PRODUCT: OpenFoodFactsProduct = {
    barcode: '4600000000000',
    name: 'Open Food Facts product',
    brand: 'OFF brand',
    category: 'Category',
    caloriesPer100G: 310.49,
    proteinsPer100G: 9.94,
    fatsPer100G: 7.15,
    carbsPer100G: 42.24,
    fiberPer100G: 3.04,
};

describe('Open Food Facts nutrition prefill mapper', () => {
    it('should build Open Food Facts lookup patch for empty add form', () => {
        expect(buildOpenFoodFactsLookupPatch(EMPTY_FORM_VALUES, OFF_PRODUCT)).toEqual({
            name: OFF_PRODUCT.name,
            brand: OFF_PRODUCT.brand,
            caloriesPerBase: 310,
            proteinsPerBase: 9.9,
            fatsPerBase: 7.2,
            carbsPerBase: 42.2,
            fiberPerBase: 3,
        });
    });

    it('should not overwrite user-entered values during barcode lookup', () => {
        const values: ProductFormValues = {
            ...EMPTY_FORM_VALUES,
            name: 'Typed name',
            brand: 'Typed brand',
            caloriesPerBase: 100,
            proteinsPerBase: 10,
            fatsPerBase: 5,
            carbsPerBase: 20,
            fiberPerBase: 2,
        };

        expect(buildOpenFoodFactsLookupPatch(values, OFF_PRODUCT)).toEqual({});
    });

    it('should build selected source product patch and overwrite nutrition values', () => {
        const suggestion: ProductSearchSuggestion = {
            source: 'openFoodFacts',
            barcode: OFF_PRODUCT.barcode,
            name: OFF_PRODUCT.name,
            brand: OFF_PRODUCT.brand,
            category: OFF_PRODUCT.category,
            caloriesPer100G: OFF_PRODUCT.caloriesPer100G,
            proteinsPer100G: OFF_PRODUCT.proteinsPer100G,
            fatsPer100G: OFF_PRODUCT.fatsPer100G,
            carbsPer100G: OFF_PRODUCT.carbsPer100G,
            fiberPer100G: OFF_PRODUCT.fiberPer100G,
        };

        expect(buildSourceProductPrefillPatch(suggestion)).toEqual({
            barcode: OFF_PRODUCT.barcode,
            name: OFF_PRODUCT.name,
            brand: OFF_PRODUCT.brand,
            caloriesPerBase: 310,
            proteinsPerBase: 9.9,
            fatsPerBase: 7.2,
            carbsPerBase: 42.2,
            fiberPerBase: 3,
        });
    });
});

describe('USDA nutrition prefill mapper', () => {
    it('should build USDA detail patch using nutrient ids, name fallback, and kJ conversion', () => {
        const energyKj = 450;
        const detail: UsdaFoodDetail = {
            fdcId: 123,
            description: 'USDA product',
            foodCategory: 'Dairy',
            nutrients: [
                {
                    nutrientId: USDA_NUTRIENT_IDS.energy,
                    name: 'Energy',
                    unit: 'kJ',
                    amountPer100g: energyKj,
                    dailyValue: null,
                    percentDailyValue: null,
                },
                {
                    nutrientId: USDA_NUTRIENT_IDS.protein,
                    name: 'Protein',
                    unit: 'g',
                    amountPer100g: 12.34,
                    dailyValue: null,
                    percentDailyValue: null,
                },
                {
                    nutrientId: 999_001,
                    name: 'Total lipid (fat)',
                    unit: 'g',
                    amountPer100g: 4.56,
                    dailyValue: null,
                    percentDailyValue: null,
                },
                {
                    nutrientId: USDA_NUTRIENT_IDS.carbs,
                    name: 'Carbohydrate, by difference',
                    unit: 'g',
                    amountPer100g: 20.05,
                    dailyValue: null,
                    percentDailyValue: null,
                },
            ],
            portions: [],
            healthScores: null,
        };

        expect(buildUsdaFoodDetailPrefillPatch(detail)).toEqual({
            name: detail.description,
            usdaFdcId: detail.fdcId,
            baseUnit: MeasurementUnit.G,
            baseAmount: DEFAULT_BASE_AMOUNT,
            caloriesPerBase: Math.round(energyKj * KJ_TO_KCAL_FACTOR),
            proteinsPerBase: 12.3,
            fatsPerBase: 4.6,
            carbsPerBase: 20.1,
        });
    });
});

describe('nutrition reset prefill mapper', () => {
    it('should build reset nutrition patch', () => {
        expect(buildResetNutritionPatch()).toEqual({
            caloriesPerBase: null,
            proteinsPerBase: null,
            fatsPerBase: null,
            carbsPerBase: null,
            fiberPerBase: null,
            alcoholPerBase: null,
        });
    });
});
