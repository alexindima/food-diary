import { describe, expect, it } from 'vitest';

import { MeasurementUnit } from '../../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import { RECIPE_DETAIL_INGREDIENT_PREVIEW_LIMIT, RECIPE_DETAIL_MACRO_SUMMARY_LIMIT } from './recipe-detail.config';
import { buildRecipeDetailViewModel } from './recipe-detail-nutrition.mapper';

const TOTAL_CALORIES = 500;
const TOTAL_PROTEINS = 30;
const TOTAL_FATS = 20;
const TOTAL_CARBS = 60;
const TOTAL_FIBER = 8;
const TOTAL_ALCOHOL = 0;
const QUALITY_SCORE = 80;
const DEFAULT_QUALITY_SCORE = 50;
const MANUAL_CALORIES = 200;
const MANUAL_PROTEINS = 10;
const MANUAL_FATS = 5;
const MANUAL_CARBS = 25;
const COMPUTED_FIBER = 4;
const COMPUTED_ALCOHOL = 1;
const MACRO_BLOCK_COUNT = 5;

describe('buildRecipeDetailViewModel nutrition', () => {
    it('builds nutrition form, macro blocks, and summary values from recipe totals', () => {
        const recipe = createRecipe();

        const viewModel = buildRecipeDetailViewModel(recipe, 'Unknown ingredient');

        expect(viewModel.calories).toBe(TOTAL_CALORIES);
        expect(viewModel.proteins).toBe(TOTAL_PROTEINS);
        expect(viewModel.fats).toBe(TOTAL_FATS);
        expect(viewModel.carbs).toBe(TOTAL_CARBS);
        expect(viewModel.fiber).toBe(TOTAL_FIBER);
        expect(viewModel.alcohol).toBe(TOTAL_ALCOHOL);
        expect(viewModel.qualityScore).toBe(QUALITY_SCORE);
        expect(viewModel.qualityGrade).toBe('green');
        expect(viewModel.macroBlocks).toHaveLength(MACRO_BLOCK_COUNT);
        expect(viewModel.macroSummaryBlocks).toHaveLength(RECIPE_DETAIL_MACRO_SUMMARY_LIMIT);
        expect(viewModel.nutritionForm.getRawValue()).toEqual({
            calories: TOTAL_CALORIES,
            proteins: TOTAL_PROTEINS,
            fats: TOTAL_FATS,
            carbs: TOTAL_CARBS,
            fiber: TOTAL_FIBER,
            alcohol: TOTAL_ALCOHOL,
        });
        expect(viewModel.macroBarState.isEmpty).toBe(false);
    });

    it('falls back to manual values and computes fiber/alcohol from ingredients when totals are absent', () => {
        const recipe = createRecipe({
            qualityScore: null,
            qualityGrade: null,
            totalCalories: null,
            totalProteins: null,
            totalFats: null,
            totalCarbs: null,
            totalFiber: null,
            totalAlcohol: null,
            manualCalories: MANUAL_CALORIES,
            manualProteins: MANUAL_PROTEINS,
            manualFats: MANUAL_FATS,
            manualCarbs: MANUAL_CARBS,
            manualFiber: null,
            manualAlcohol: null,
        });

        const viewModel = buildRecipeDetailViewModel(recipe, 'Unknown ingredient');

        expect(viewModel.calories).toBe(MANUAL_CALORIES);
        expect(viewModel.proteins).toBe(MANUAL_PROTEINS);
        expect(viewModel.fats).toBe(MANUAL_FATS);
        expect(viewModel.carbs).toBe(MANUAL_CARBS);
        expect(viewModel.fiber).toBe(COMPUTED_FIBER);
        expect(viewModel.alcohol).toBe(COMPUTED_ALCOHOL);
        expect(viewModel.qualityScore).toBe(DEFAULT_QUALITY_SCORE);
        expect(viewModel.qualityGrade).toBe('yellow');
    });
});

describe('buildRecipeDetailViewModel preview', () => {
    it('builds ingredient preview with limits, units, and unknown fallback', () => {
        const recipe = createRecipe({
            steps: [
                {
                    id: 'step-1',
                    stepNumber: 1,
                    title: null,
                    instruction: 'Mix',
                    imageUrl: null,
                    imageAssetId: null,
                    ingredients: Array.from({ length: RECIPE_DETAIL_INGREDIENT_PREVIEW_LIMIT + 1 }, (_, index) => ({
                        id: `ingredient-${index}`,
                        amount: index + 1,
                        productName: index === 0 ? null : `Ingredient ${index}`,
                        nestedRecipeName: index === 0 ? null : undefined,
                        productBaseUnit: index === 1 ? '' : MeasurementUnit.G,
                    })),
                },
            ],
        });

        const viewModel = buildRecipeDetailViewModel(recipe, 'Unknown ingredient');

        expect(viewModel.ingredientPreview).toHaveLength(RECIPE_DETAIL_INGREDIENT_PREVIEW_LIMIT);
        expect(viewModel.ingredientPreview[0]).toEqual({
            name: 'Unknown ingredient',
            amount: 1,
            unitKey: `GENERAL.UNITS.${MeasurementUnit.G}`,
        });
        expect(viewModel.ingredientPreview[1].unitKey).toBeNull();
        expect(viewModel.ingredientCount).toBe(RECIPE_DETAIL_INGREDIENT_PREVIEW_LIMIT + 1);
    });

    it('returns null total time when both prep and cook time are missing', () => {
        const viewModel = buildRecipeDetailViewModel(createRecipe({ prepTime: null, cookTime: null }), 'Unknown ingredient');

        expect(viewModel.totalTime).toBeNull();
    });
});

function createRecipe(overrides: Partial<Recipe> = {}): Recipe {
    return {
        id: 'recipe-1',
        name: 'Recipe',
        description: null,
        comment: null,
        category: null,
        imageUrl: null,
        imageAssetId: null,
        prepTime: 10,
        cookTime: 20,
        servings: 2,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        qualityScore: QUALITY_SCORE,
        qualityGrade: 'green',
        totalCalories: TOTAL_CALORIES,
        totalProteins: TOTAL_PROTEINS,
        totalFats: TOTAL_FATS,
        totalCarbs: TOTAL_CARBS,
        totalFiber: TOTAL_FIBER,
        totalAlcohol: TOTAL_ALCOHOL,
        isNutritionAutoCalculated: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        steps: [
            {
                id: 'step-1',
                stepNumber: 1,
                title: null,
                instruction: 'Mix',
                imageUrl: null,
                imageAssetId: null,
                ingredients: [
                    {
                        id: 'ingredient-1',
                        amount: 200,
                        productId: 'product-1',
                        productName: 'Flour',
                        productBaseUnit: MeasurementUnit.G,
                        productBaseAmount: 100,
                        productCaloriesPerBase: 364,
                        productProteinsPerBase: 10,
                        productFatsPerBase: 1,
                        productCarbsPerBase: 76,
                        productFiberPerBase: 2,
                        productAlcoholPerBase: 0.5,
                    },
                ],
            },
        ],
        ...overrides,
    };
}
