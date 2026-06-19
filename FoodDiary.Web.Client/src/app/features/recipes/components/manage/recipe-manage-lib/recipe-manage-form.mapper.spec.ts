import { describe, expect, it } from 'vitest';

import { MeasurementUnit, ProductVisibility } from '../../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import type { NutritionScaleMode, RecipeFormValues } from './recipe-manage.types';
import {
    buildRecipeDto,
    buildRecipeFormPatchValue,
    createRecipeFormValue,
    createRecipeIngredientValue,
    createRecipeStepValue,
    hasNoRecipeNutritionTotals,
    mapRecipeStepToFormValue,
    normalizeRecipeVisibility,
} from './recipe-manage-form.mapper';

const DEFAULT_BASE_AMOUNT = 100;
const DEFAULT_SERVINGS = 2;
const PRODUCT_AMOUNT = 150;
const RECIPE_TOTAL_FACTOR = 10;

const RECIPE: Recipe = {
    id: 'recipe-1',
    name: 'Test recipe',
    description: 'Description',
    comment: 'Comment',
    category: 'Dinner',
    imageUrl: 'https://example.test/recipe.jpg',
    imageAssetId: 'asset-1',
    prepTime: 15,
    cookTime: 30,
    servings: DEFAULT_SERVINGS,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: '2026-01-01T00:00:00Z',
    isOwnedByCurrentUser: true,
    totalCalories: 500,
    totalProteins: 40,
    totalFats: 20,
    totalCarbs: 60,
    totalFiber: 6,
    totalAlcohol: 0,
    isNutritionAutoCalculated: false,
    manualCalories: null,
    manualProteins: 35,
    manualFats: null,
    manualCarbs: null,
    manualFiber: null,
    manualAlcohol: null,
    steps: [],
};

describe('recipe manage form creation', () => {
    it('should create form value with empty steps array and default values', () => {
        const form = createRecipeFormValue();

        expect(form.name).toBe('');
        expect(form.servings).toBe(1);
        expect(form.visibility).toBe(RecipeVisibility.Public);
        expect(form.calculateNutritionAutomatically).toBe(true);
        expect(form.steps.length).toBe(0);
    });

    it('should create a step value with one empty ingredient when no values are provided', () => {
        const step = createRecipeStepValue();

        expect(step.title).toBeNull();
        expect(step.description).toBe('');
        expect(step.ingredients.length).toBe(1);
        expect(step.ingredients[0]?.foodName).toBeNull();
    });

    it('should create ingredient value from selected product defaults', () => {
        const ingredient = createRecipeIngredientValue({
            food: {
                id: 'product-1',
                name: 'Product',
                baseUnit: MeasurementUnit.G,
                baseAmount: DEFAULT_BASE_AMOUNT,
                defaultPortionAmount: PRODUCT_AMOUNT,
                caloriesPerBase: 100,
                proteinsPerBase: 10,
                fatsPerBase: 5,
                carbsPerBase: 15,
                fiberPerBase: 2,
                alcoholPerBase: 0,
                productType: undefined,
                barcode: null,
                brand: null,
                category: null,
                description: null,
                imageUrl: null,
                usageCount: 0,
                visibility: ProductVisibility.Private,
                createdAt: new Date('2026-01-01T00:00:00Z'),
                isOwnedByCurrentUser: true,
                qualityScore: 50,
                qualityGrade: 'yellow',
            },
        });

        expect(ingredient.foodName).toBe('Product');
        expect(ingredient.productId).toBe('product-1');
        expect(ingredient.amount).toBeNull();
    });
});

describe('recipe manage DTO mapping', () => {
    it('should build recipe DTO and scale manual nutrition through provided converter', () => {
        const formValue = createManualRecipeFormValue();

        expect(buildRecipeDto(formValue, 'portion', DEFAULT_SERVINGS, scaleValue)).toEqual({
            name: formValue.name,
            description: formValue.description,
            comment: null,
            category: formValue.category,
            imageUrl: formValue.imageUrl?.url,
            imageAssetId: formValue.imageUrl?.assetId,
            prepTime: null,
            cookTime: formValue.cookTime,
            servings: DEFAULT_SERVINGS,
            visibility: RecipeVisibility.Private,
            calculateNutritionAutomatically: false,
            manualCalories: 500,
            manualProteins: 40,
            manualFats: 20,
            manualCarbs: 80,
            manualFiber: 10,
            manualAlcohol: 0,
            steps: [
                {
                    title: 'Step',
                    imageUrl: null,
                    imageAssetId: null,
                    description: 'Cook',
                    ingredients: [
                        {
                            productId: undefined,
                            nestedRecipeId: 'nested-1',
                            amount: DEFAULT_SERVINGS,
                        },
                    ],
                },
            ],
        });
    });

    it('should null manual nutrition totals when automatic calculation is enabled', () => {
        const formValue: RecipeFormValues = {
            ...createRecipeFormValue(),
            name: 'Auto recipe',
            cookTime: 10,
            calculateNutritionAutomatically: true,
        };

        expect(buildRecipeDto(formValue, 'recipe', 1, scaleValue).manualCalories).toBeNull();
    });

    it('should preserve empty optional timing fields as null in DTO', () => {
        const formValue: RecipeFormValues = {
            ...createRecipeFormValue(),
            name: 'Recipe without timing',
            prepTime: null,
            cookTime: null,
        };

        const dto = buildRecipeDto(formValue, 'recipe', 1, scaleValue);

        expect(dto.prepTime).toBeNull();
        expect(dto.cookTime).toBeNull();
    });

    it('should preserve selected product id when product object is not available', () => {
        const formValue = createManualRecipeFormValue();
        formValue.steps[0].ingredients[0] = {
            food: null,
            productId: 'product-1',
            amount: PRODUCT_AMOUNT,
            foodName: 'Product',
            nestedRecipe: null,
            nestedRecipeId: null,
            nestedRecipeName: null,
        };

        const dto = buildRecipeDto(formValue, 'recipe', 1, scaleValue);

        expect(dto.steps[0]?.ingredients[0]).toEqual({
            productId: 'product-1',
            nestedRecipeId: undefined,
            amount: PRODUCT_AMOUNT,
        });
    });
});

describe('recipe manage edit mapping', () => {
    it('should build form patch from existing recipe and prefer manual values over totals', () => {
        expect(buildRecipeFormPatchValue(RECIPE)).toEqual({
            name: RECIPE.name,
            description: RECIPE.description,
            comment: RECIPE.comment,
            category: RECIPE.category,
            imageUrl: {
                url: RECIPE.imageUrl,
                assetId: RECIPE.imageAssetId,
            },
            prepTime: RECIPE.prepTime,
            cookTime: RECIPE.cookTime,
            servings: RECIPE.servings,
            visibility: RecipeVisibility.Private,
            calculateNutritionAutomatically: false,
            manualCalories: RECIPE.totalCalories,
            manualProteins: RECIPE.manualProteins,
            manualFats: RECIPE.totalFats,
            manualCarbs: RECIPE.totalCarbs,
            manualFiber: RECIPE.totalFiber,
            manualAlcohol: RECIPE.totalAlcohol,
        });
    });

    it('should map recipe step ingredients to product and nested recipe form values', () => {
        const step = mapRecipeStepToFormValue(
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
                        amount: PRODUCT_AMOUNT,
                        productId: 'product-1',
                        productName: 'Flour',
                        productBaseUnit: 'INVALID',
                        productBaseAmount: DEFAULT_BASE_AMOUNT,
                        productCaloriesPerBase: 350,
                    },
                    {
                        id: 'ingredient-2',
                        amount: 1,
                        nestedRecipeId: 'nested-1',
                        nestedRecipeName: null,
                    },
                ],
            },
            {
                selectIngredient: 'Select ingredient',
                unknownProduct: 'Unknown product',
            },
        );

        expect(step.ingredients[0]?.food?.name).toBe('Flour');
        expect(step.ingredients[0]?.food?.baseUnit).toBe(MeasurementUnit.G);
        expect(step.ingredients[1]?.foodName).toBe('Select ingredient');
        expect(step.ingredients[1]?.nestedRecipeId).toBe('nested-1');
    });
});

describe('recipe manage utility mapping', () => {
    it('should detect missing nutrition totals', () => {
        expect(
            hasNoRecipeNutritionTotals({
                ...RECIPE,
                totalCalories: null,
                totalProteins: null,
                totalFats: null,
                totalCarbs: null,
            }),
        ).toBe(true);
        expect(hasNoRecipeNutritionTotals(RECIPE)).toBe(false);
    });

    it('should normalize visibility defensively', () => {
        expect(normalizeRecipeVisibility(null)).toBe(RecipeVisibility.Public);
        expect(normalizeRecipeVisibility('private')).toBe(RecipeVisibility.Private);
        expect(normalizeRecipeVisibility('unknown')).toBe(RecipeVisibility.Public);
    });
});

function scaleValue(value: number | null | undefined, scaleMode: NutritionScaleMode): number {
    const numeric = value ?? 0;
    return scaleMode === 'portion' ? numeric * RECIPE_TOTAL_FACTOR : numeric;
}

function createManualRecipeFormValue(): RecipeFormValues {
    return {
        name: 'Recipe',
        description: '',
        comment: null,
        category: 'Dinner',
        imageUrl: { url: 'https://example.test/image.jpg', assetId: 'asset-2' },
        prepTime: null,
        cookTime: 45,
        servings: DEFAULT_SERVINGS,
        visibility: RecipeVisibility.Private,
        calculateNutritionAutomatically: false,
        manualCalories: 50,
        manualProteins: 4,
        manualFats: 2,
        manualCarbs: 8,
        manualFiber: 1,
        manualAlcohol: 0,
        steps: [
            {
                title: 'Step',
                imageUrl: null,
                description: 'Cook',
                ingredients: [
                    {
                        food: null,
                        productId: null,
                        amount: DEFAULT_SERVINGS,
                        foodName: 'Nested recipe',
                        nestedRecipe: null,
                        nestedRecipeId: 'nested-1',
                        nestedRecipeName: 'Nested recipe',
                    },
                ],
            },
        ],
    };
}
