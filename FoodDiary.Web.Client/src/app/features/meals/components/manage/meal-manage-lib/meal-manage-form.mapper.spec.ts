import { FormControl, FormGroup, Validators } from '@angular/forms';
import { describe, expect, it, vi } from 'vitest';

import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    ConsumptionSourceType,
    createEmptyProductSnapshot,
    createEmptyRecipeSnapshot,
} from '../../../models/meal.data';
import type { ConsumptionFormValues, ConsumptionItemFormData, ConsumptionItemFormValues, NutritionTotals } from './meal-manage.types';
import {
    buildMealManageDto,
    buildMealManageFormPatchValue,
    createMealManageForm,
    getConsumptionItemInitialAmount,
} from './meal-manage-form.mapper';

const PRODUCT_AMOUNT = 150;
const RECIPE_AMOUNT_GRAMS = 200;
const RECIPE_AMOUNT_SERVINGS = 2;
const FIXED_DATE = new Date('2026-04-05T09:07:00');
const SUBMIT_DATE = new Date('2026-04-05T10:30:00');
const MANUAL_TOTALS: NutritionTotals = {
    calories: 500,
    proteins: 40,
    fats: 20,
    carbs: 60,
    fiber: 8,
    alcohol: 1,
};
const AI_SESSIONS: ConsumptionAiSessionManageDto[] = [
    {
        notes: 'recognized',
        items: [],
    },
];

const product = {
    ...createEmptyProductSnapshot(),
    id: 'product-1',
};
const recipe = {
    ...createEmptyRecipeSnapshot(),
    id: 'recipe-1',
};
const image: ImageSelection = {
    url: 'https://example.test/meal.jpg',
    assetId: 'asset-1',
};

describe('meal manage form creation', () => {
    it('should create form with fixed date, time and one item', () => {
        const form = createMealManageForm(
            {
                createItem: () => createItemGroup(createProductItem()),
                createItemsValidator: () => Validators.required,
            },
            FIXED_DATE,
        );

        expect(form.controls.date.value).toBe('2026-04-05');
        expect(form.controls.time.value).toBe('09:07');
        expect(form.controls.items.length).toBe(1);
        expect(form.controls.isNutritionAutoCalculated.value).toBe(true);
    });
});

describe('meal manage DTO mapping', () => {
    it('should build auto nutrition DTO with product item, image and ai sessions', () => {
        const formValue = {
            ...createBaseFormValue(),
            imageUrl: image,
            items: [createProductItem()],
        };

        expect(buildMealManageDto(formValue, createDtoCallbacks())).toEqual({
            date: SUBMIT_DATE,
            mealType: 'Breakfast',
            comment: 'Comment',
            imageUrl: image.url,
            imageAssetId: image.assetId,
            items: [{ productId: product.id, recipeId: null, amount: PRODUCT_AMOUNT }],
            aiSessions: AI_SESSIONS,
            isNutritionAutoCalculated: true,
            manualCalories: undefined,
            manualProteins: undefined,
            manualFats: undefined,
            manualCarbs: undefined,
            manualFiber: undefined,
            manualAlcohol: undefined,
            preMealSatietyLevel: 4,
            postMealSatietyLevel: 3,
        });
    });

    it('should build manual nutrition DTO and convert recipe grams to servings', () => {
        const convertRecipeGramsToServings = vi.fn().mockReturnValue(RECIPE_AMOUNT_SERVINGS);
        const formValue = {
            ...createBaseFormValue(),
            isNutritionAutoCalculated: false,
            items: [
                {
                    sourceType: ConsumptionSourceType.Recipe,
                    product: null,
                    recipe,
                    amount: RECIPE_AMOUNT_GRAMS,
                },
            ],
        };

        const dto = buildMealManageDto(formValue, {
            ...createDtoCallbacks(),
            convertRecipeGramsToServings,
        });

        expect(convertRecipeGramsToServings).toHaveBeenCalledWith(recipe, RECIPE_AMOUNT_GRAMS);
        expect(dto.items).toEqual([{ recipeId: recipe.id, productId: null, amount: RECIPE_AMOUNT_SERVINGS }]);
        expect(dto.manualCalories).toBe(MANUAL_TOTALS.calories);
        expect(dto.manualAlcohol).toBe(MANUAL_TOTALS.alcohol);
    });
});

describe('meal manage edit mapping', () => {
    it('should build form patch and prefer manual nutrition over totals', () => {
        const consumption: Consumption = {
            id: 'consumption-1',
            date: '2026-04-05T09:07:00',
            mealType: 'breakfast',
            comment: 'Comment',
            imageUrl: image.url,
            imageAssetId: image.assetId,
            totalCalories: 600,
            totalProteins: 50,
            totalFats: 25,
            totalCarbs: 70,
            totalFiber: 10,
            totalAlcohol: 2,
            isNutritionAutoCalculated: false,
            manualCalories: null,
            manualProteins: 42,
            manualFats: null,
            manualCarbs: null,
            manualFiber: null,
            manualAlcohol: 0,
            preMealSatietyLevel: 3,
            postMealSatietyLevel: 7,
            items: [],
        };

        expect(buildMealManageFormPatchValue(consumption)).toEqual({
            date: '2026-04-05',
            time: '09:07',
            mealType: 'BREAKFAST',
            comment: consumption.comment,
            imageUrl: image,
            isNutritionAutoCalculated: false,
            manualCalories: consumption.totalCalories,
            manualProteins: consumption.manualProteins,
            manualFats: consumption.totalFats,
            manualCarbs: consumption.totalCarbs,
            manualFiber: consumption.totalFiber,
            manualAlcohol: consumption.manualAlcohol,
            preMealSatietyLevel: consumption.preMealSatietyLevel,
            postMealSatietyLevel: 4,
        });
    });

    it('should convert initial amount only for recipe items', () => {
        const convertRecipeServingsToGrams = vi.fn().mockReturnValue(RECIPE_AMOUNT_GRAMS);

        expect(
            getConsumptionItemInitialAmount(
                {
                    id: 'item-1',
                    consumptionId: 'consumption-1',
                    amount: RECIPE_AMOUNT_SERVINGS,
                    sourceType: ConsumptionSourceType.Recipe,
                    recipe,
                },
                convertRecipeServingsToGrams,
            ),
        ).toBe(RECIPE_AMOUNT_GRAMS);
        expect(convertRecipeServingsToGrams).toHaveBeenCalled();
        expect(
            getConsumptionItemInitialAmount(
                {
                    id: 'item-2',
                    consumptionId: 'consumption-1',
                    amount: PRODUCT_AMOUNT,
                    sourceType: ConsumptionSourceType.Product,
                    product,
                },
                convertRecipeServingsToGrams,
            ),
        ).toBe(PRODUCT_AMOUNT);
    });
});

function createBaseFormValue(): ConsumptionFormValues {
    return {
        date: '2026-04-05',
        time: '10:30',
        mealType: 'Breakfast',
        items: [],
        comment: 'Comment',
        imageUrl: null,
        isNutritionAutoCalculated: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        preMealSatietyLevel: 4,
        postMealSatietyLevel: 6,
    };
}

function createProductItem(): ConsumptionItemFormValues {
    return {
        sourceType: ConsumptionSourceType.Product,
        product,
        recipe: null,
        amount: PRODUCT_AMOUNT,
    };
}

function createItemGroup(value: ConsumptionItemFormValues): FormGroup<ConsumptionItemFormData> {
    return new FormGroup<ConsumptionItemFormData>({
        sourceType: new FormControl(value.sourceType, { nonNullable: true }),
        product: new FormControl(value.product),
        recipe: new FormControl(value.recipe),
        amount: new FormControl(value.amount),
    });
}

function createDtoCallbacks(): Parameters<typeof buildMealManageDto>[1] {
    return {
        aiSessions: AI_SESSIONS,
        buildDateTime: () => SUBMIT_DATE,
        convertRecipeGramsToServings: () => RECIPE_AMOUNT_SERVINGS,
        manualTotals: MANUAL_TOTALS,
    };
}
