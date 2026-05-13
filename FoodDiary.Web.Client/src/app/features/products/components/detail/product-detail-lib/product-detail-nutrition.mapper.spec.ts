import { describe, expect, it } from 'vitest';

import { CHART_COLORS } from '../../../../../constants/chart-colors';
import { DEFAULT_NUTRITION_BASE_AMOUNT, PERCENT_MULTIPLIER } from '../../../../../shared/lib/nutrition.constants';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../models/product.data';
import { buildProductDetailNutritionViewModel } from './product-detail-nutrition.mapper';

const PRODUCT_CALORIES = 250;
const PRODUCT_PROTEINS = 20;
const PRODUCT_FATS = 10;
const PRODUCT_CARBS = 30;
const PRODUCT_FIBER = 4;
const PRODUCT_ALCOHOL = 0;
const QUALITY_SCORE_GREEN = 80;
const MIN_MACRO_BAR_PERCENT = 4;
const MACRO_BLOCK_COUNT = 5;
const MACRO_SUMMARY_COUNT = 3;

describe('buildProductDetailNutritionViewModel', () => {
    it('should build readonly nutrition form from product values', () => {
        const viewModel = buildProductDetailNutritionViewModel(createProduct());

        expect(viewModel.nutritionForm.getRawValue()).toEqual({
            calories: PRODUCT_CALORIES,
            proteins: PRODUCT_PROTEINS,
            fats: PRODUCT_FATS,
            carbs: PRODUCT_CARBS,
            fiber: PRODUCT_FIBER,
            alcohol: PRODUCT_ALCOHOL,
        });
    });

    it('should build macro bar state and macro blocks', () => {
        const viewModel = buildProductDetailNutritionViewModel(createProduct());

        expect(viewModel.macroBarState.isEmpty).toBe(false);
        expect(viewModel.macroBarState.segments.map(segment => segment.key)).toEqual(['proteins', 'fats', 'carbs']);
        expect(viewModel.macroBlocks).toHaveLength(MACRO_BLOCK_COUNT);
        expect(viewModel.macroSummaryBlocks).toHaveLength(MACRO_SUMMARY_COUNT);
        expect(viewModel.macroBlocks[0]).toEqual({
            labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
            value: PRODUCT_PROTEINS,
            unitKey: 'GENERAL.UNITS.G',
            color: CHART_COLORS.proteins,
            percent: Math.round((PRODUCT_PROTEINS / PRODUCT_CARBS) * PERCENT_MULTIPLIER),
        });
    });

    it('should keep tiny macro block percentages visible', () => {
        const viewModel = buildProductDetailNutritionViewModel(createProduct({ proteinsPerBase: 0 }));

        expect(viewModel.macroBlocks[0].percent).toBe(MIN_MACRO_BAR_PERCENT);
    });
});

function createProduct(overrides: Partial<Product> = {}): Product {
    return {
        id: 'product-1',
        name: 'Test Product',
        barcode: null,
        brand: null,
        productType: ProductType.Other,
        category: null,
        description: null,
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        baseUnit: MeasurementUnit.G,
        baseAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
        defaultPortionAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
        caloriesPerBase: PRODUCT_CALORIES,
        proteinsPerBase: PRODUCT_PROTEINS,
        fatsPerBase: PRODUCT_FATS,
        carbsPerBase: PRODUCT_CARBS,
        fiberPerBase: PRODUCT_FIBER,
        alcoholPerBase: PRODUCT_ALCOHOL,
        usageCount: 0,
        visibility: ProductVisibility.Private,
        createdAt: new Date('2026-01-01T00:00:00Z'),
        isOwnedByCurrentUser: true,
        qualityScore: QUALITY_SCORE_GREEN,
        qualityGrade: 'green',
        ...overrides,
    };
}
