import { describe, expect, it } from 'vitest';

import { imageSelection } from '../../../../../shared/models/image-upload.data';
import { MeasurementUnit, ProductVisibility } from '../../../../../shared/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../../shared/models/recipe.data';
import { recipeIngredientFromStored } from '../../../../../shared/models/recipe-ingredient';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
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

describe('text ingredient persistence', () => {
    it('round trips a text ingredient with a free-form amount', () => {
        const step = mapRecipeStepToFormValue(
            {
                id: 'step',
                stepNumber: 1,
                instruction: 'Season',
                ingredients: [recipeIngredientFromStored({ id: 'salt', amount: 0, textName: 'Salt', amountText: 'to taste' })],
            },
            { selectIngredient: 'Select', unknownProduct: 'Unknown' },
        );
        expect(step.ingredients[0].textName).toBe('Salt');
        const dto = buildRecipeDto({ ...createRecipeFormValue(), steps: [step] }, 'recipe', 1, value => value ?? 0);
        expect(dto.steps[0].ingredients).toEqual([
            { productId: undefined, nestedRecipeId: undefined, amount: 0, textName: 'Salt', amountText: 'to taste' },
        ]);
    });

    it('keeps a text ingredient with no amount', () => {
        const ingredient = createRecipeIngredientValue({ textName: 'Salt' });
        const dto = buildRecipeDto(
            { ...createRecipeFormValue(), steps: [{ ...createRecipeStepValue(), description: 'Season', ingredients: [ingredient] }] },
            'recipe',
            1,
            value => value ?? 0,
        );
        expect(dto.steps[0].ingredients[0]).toMatchObject({ textName: 'Salt', amountText: null, amount: 0 });
    });
});

const RECIPE: Recipe = {
    id: entityId<'recipe'>('recipe-1'),
    name: 'Test recipe',
    description: 'Description',
    comment: 'Comment',
    category: 'main_courses',
    imageUrl: 'https://example.test/recipe.jpg',
    imageAssetId: entityId<'image-asset'>('asset-1'),
    prepTime: 15,
    cookTime: 30,
    servings: DEFAULT_SERVINGS,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: utcInstant('2026-01-01T00:00:00Z'),
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
        expect(form.visibility).toBe(RecipeVisibility.Private);
        expect(form.calculateNutritionAutomatically).toBe(true);
        expect(form.steps.length).toBe(0);
    });

    it('should create a step without ingredients when no values are provided', () => {
        const step = createRecipeStepValue();

        expect(step.title).toBeNull();
        expect(step.description).toBe('');
        expect(step.ingredients).toEqual([]);
    });

    it('should create ingredient value from selected product defaults', () => {
        const ingredient = createRecipeIngredientValue({
            food: {
                id: entityId<'product'>('product-1'),
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
            language: 'en',
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
            language: 'en',
            name: RECIPE.name,
            description: RECIPE.description,
            comment: RECIPE.comment,
            category: RECIPE.category,
            imageUrl: imageSelection(RECIPE.imageUrl, RECIPE.imageAssetId),
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
});

describe('recipe snapshot unit casing', () => {
    it.each([
        ['Ml', MeasurementUnit.ML],
        ['Pcs', MeasurementUnit.PCS],
        ['ML', MeasurementUnit.ML],
        ['PCS', MeasurementUnit.PCS],
    ])('preserves API snapshot unit %s as %s when reopening the recipe editor', (unit, expectedUnit) => {
        const step = mapRecipeStepToFormValue(
            {
                id: 'unit-step',
                stepNumber: 1,
                title: null,
                instruction: 'Mix',
                imageUrl: null,
                imageAssetId: null,
                ingredients: [
                    recipeIngredientFromStored({
                        id: 'unit-ingredient',
                        amount: PRODUCT_AMOUNT,
                        productId: 'product-unit',
                        productName: 'Milk',
                        productBaseUnit: unit,
                        productBaseAmount: DEFAULT_BASE_AMOUNT,
                    }),
                ],
            },
            { selectIngredient: 'Select ingredient', unknownProduct: 'Unknown product' },
        );
        expect(step.ingredients[0]?.food?.baseUnit).toBe(expectedUnit);
        expect(step.ingredients[0]?.amount).toBe(PRODUCT_AMOUNT);
    });
});

describe('recipe ingredient edit mapping', () => {
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
                    recipeIngredientFromStored({
                        id: 'ingredient-1',
                        amount: PRODUCT_AMOUNT,
                        productId: 'product-1',
                        productName: 'Flour',
                        productBaseUnit: 'INVALID',
                        productBaseAmount: DEFAULT_BASE_AMOUNT,
                        productCaloriesPerBase: 350,
                    }),
                    recipeIngredientFromStored({
                        id: 'ingredient-2',
                        amount: 1,
                        nestedRecipeId: 'nested-1',
                        nestedRecipeName: null,
                    }),
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
        language: 'en',
        description: '',
        comment: null,
        category: 'main_courses',
        imageUrl: imageSelection('https://example.test/image.jpg', 'asset-2'),
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

describe('recipe gallery mapping', () => {
    it('restores every image in order and sends ordered asset ids, including explicit removal', () => {
        const images = [
            { imageAssetId: 'front', imageUrl: '/front.jpg' },
            { imageAssetId: 'back', imageUrl: '/back.jpg' },
        ];
        const values = { ...createRecipeFormValue(), ...buildRecipeFormPatchValue({ ...RECIPE, images }) };
        expect(values.images?.map(image => image.assetId)).toEqual(['front', 'back']);
        expect(buildRecipeDto(values, 'recipe', 1, value => value ?? 0).imageAssetIds).toEqual(['front', 'back']);
        expect(buildRecipeDto({ ...values, images: [], imageUrl: null }, 'recipe', 1, value => value ?? 0).imageAssetIds).toEqual([]);
        expect(buildRecipeDto(createRecipeFormValue(), 'recipe', 1, value => value ?? 0).imageAssetIds).toBeUndefined();
    });
});

describe('step gallery mapping', () => {
    it('preserves step photos through editing and sends reordered or cleared galleries', () => {
        const step = createRecipeStepValue({
            ...createRecipeStepValue(),
            images: [imageSelection('/2.jpg', 'second'), imageSelection('/1.jpg', 'first')],
        });
        const values = createManualRecipeFormValue();
        step.ingredients = values.steps[0].ingredients;
        const dto = buildRecipeDto({ ...values, steps: [step] }, 'recipe', 1, value => value ?? 0);
        expect(dto.steps[0].imageAssetIds).toEqual(['second', 'first']);
        expect(
            buildRecipeDto({ ...values, steps: [{ ...step, images: [] }] }, 'recipe', 1, value => value ?? 0).steps[0].imageAssetIds,
        ).toEqual([]);
    });
});

describe('instruction-only step mapping', () => {
    it('preserves ingredient-free steps and their order in the save request', () => {
        const value = createManualRecipeFormValue();
        value.steps.unshift(createRecipeStepValue({ title: null, imageUrl: null, description: 'Preheat oven', ingredients: [] }));
        const dto = buildRecipeDto(value, 'recipe', DEFAULT_SERVINGS, scaleValue);
        expect(dto.steps.map(step => step.description)).toEqual(['Preheat oven', 'Cook']);
        expect(dto.steps[0].ingredients).toEqual([]);
    });
});

it('publishes ingredient descriptions only when the author saves a public recipe', () => {
    const ingredient = createRecipeIngredientValue({ nestedRecipeId: 'nested', nestedRecipeName: 'Private sauce', amount: 2 });
    const form = { ...createRecipeFormValue(), steps: [{ ...createRecipeStepValue(), ingredients: [ingredient] }] };
    const privateDto = buildRecipeDto(form, 'recipe', 1, value => value ?? 0);
    expect(privateDto.steps[0].ingredients[0].publicName).toBeUndefined();
    const publicDto = buildRecipeDto({ ...form, visibility: RecipeVisibility.Public }, 'recipe', 1, value => value ?? 0);
    expect(publicDto.steps[0].ingredients[0]).toMatchObject({ publicName: 'Private sauce', publicUnit: 'serving', amount: 2 });
});
