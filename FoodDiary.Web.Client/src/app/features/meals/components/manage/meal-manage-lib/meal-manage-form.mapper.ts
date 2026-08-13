import { normalizeMealType } from '../../../../../shared/lib/meal-type.util';
import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../../../shared/lib/satiety-level.utils';
import type { Recipe } from '../../../../recipes/models/recipe.data';
import { getDateInputValue, getTimeInputValue } from '../../../lib/meal-date-input.utils';
import {
    type Meal,
    type MealAiSessionManageDto,
    type MealItem,
    type MealItemManageDto,
    type MealManageDto,
    MealSourceType,
} from '../../../models/meal.data';
import type { MealFormValues, MealItemFormValues, NutritionTotals } from './meal-manage.types';

export type MealManageDtoCallbacks = {
    aiSessions: MealManageDto['aiSessions'];
    buildDateTime: () => Date;
    convertRecipeGramsToServings: (recipe: Recipe, amount: number) => number;
    manualTotals: NutritionTotals;
};

export type MealManageFormPatchValue = Partial<MealFormValues>;

export function createMealManageFormValue(now = new Date()): MealFormValues {
    return {
        date: getDateInputValue(now),
        time: getTimeInputValue(now),
        mealType: null,
        items: [createMealItemValue()],
        comment: null,
        imageUrl: null,
        isNutritionAutoCalculated: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        preMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
        postMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
    };
}

export function createMealItemValue(
    product: MealItemFormValues['product'] = null,
    recipe: MealItemFormValues['recipe'] = null,
    amount: number | null = null,
    sourceType: MealSourceType = MealSourceType.Product,
): MealItemFormValues {
    return {
        sourceType,
        product,
        recipe,
        amount,
    };
}

export function buildMealDateTime(dateValue: string, timeValue: string, fallback: Date): Date {
    const parsed = new Date(`${dateValue}T${timeValue}`);
    return Number.isNaN(parsed.getTime()) ? fallback : parsed;
}

export function findReusableEmptyMealItemIndex(items: readonly MealItemFormValues[]): number {
    return items.findIndex(item => item.product === null && item.recipe === null);
}

export function hasSelectedMealItems(items: readonly MealItemFormValues[], aiSessions: readonly MealAiSessionManageDto[]): boolean {
    return items.some(item => item.product !== null || item.recipe !== null) || aiSessions.length > 0;
}

export function buildMealManageDto(formValue: MealFormValues, callbacks: MealManageDtoCallbacks): MealManageDto {
    const image = formValue.imageUrl;
    const isNutritionAutoCalculated = formValue.isNutritionAutoCalculated;

    return {
        date: callbacks.buildDateTime(),
        mealType: formValue.mealType ?? undefined,
        comment: formValue.comment ?? undefined,
        imageUrl: image?.url ?? undefined,
        imageAssetId: image?.assetId ?? undefined,
        items: mapMealItems(formValue.items, callbacks.convertRecipeGramsToServings),
        aiSessions: callbacks.aiSessions,
        isNutritionAutoCalculated,
        ...buildManualNutritionPayload(isNutritionAutoCalculated, callbacks.manualTotals),
        preMealSatietyLevel: normalizeSatietyLevel(formValue.preMealSatietyLevel),
        postMealSatietyLevel: normalizeSatietyLevel(formValue.postMealSatietyLevel),
    };
}

export function buildMealManageFormPatchValue(meal: Meal): MealManageFormPatchValue {
    const date = new Date(meal.date);
    return {
        date: getDateInputValue(date),
        time: getTimeInputValue(date),
        mealType: normalizeMealType(meal.mealType),
        comment: toNullable(meal.comment),
        imageUrl: {
            url: toNullable(meal.imageUrl),
            assetId: toNullable(meal.imageAssetId),
        },
        isNutritionAutoCalculated: meal.isNutritionAutoCalculated,
        ...buildMealManualNutritionPatchValue(meal),
        preMealSatietyLevel: normalizeSatietyLevel(toNullable(meal.preMealSatietyLevel)),
        postMealSatietyLevel: normalizeSatietyLevel(toNullable(meal.postMealSatietyLevel)),
    };
}

export function getMealItemInitialAmount(item: MealItem, convertRecipeServingsToGrams: (item: MealItem) => number): number {
    return item.sourceType === MealSourceType.Recipe ? convertRecipeServingsToGrams(item) : item.amount;
}

export { getDateInputValue, getTimeInputValue } from '../../../lib/meal-date-input.utils';

function mapMealItems(
    items: MealItemFormValues[],
    convertRecipeGramsToServings: (recipe: Recipe, amount: number) => number,
): MealItemManageDto[] {
    return items.flatMap(item => mapMealItem(item, convertRecipeGramsToServings));
}

function mapMealItem(
    item: MealItemFormValues,
    convertRecipeGramsToServings: (recipe: Recipe, amount: number) => number,
): MealItemManageDto[] {
    const amount = normalizeItemAmount(item.amount);
    const sourceType = item.sourceType;

    if (sourceType === MealSourceType.Product && item.product !== null) {
        return [{ productId: item.product.id, recipeId: null, amount, origin: 'Manual' }];
    }

    if (sourceType === MealSourceType.Recipe && item.recipe !== null) {
        return [
            {
                recipeId: item.recipe.id,
                productId: null,
                amount: convertRecipeGramsToServings(item.recipe, amount),
                origin: 'Manual',
            },
        ];
    }

    return [];
}

function normalizeItemAmount(value: unknown): number {
    const parsedAmount = Number(value);
    return parsedAmount === 0 || Number.isNaN(parsedAmount) ? 0 : parsedAmount;
}

function buildManualNutritionPayload(isNutritionAutoCalculated: boolean, manualTotals: NutritionTotals): Partial<MealManageDto> {
    return {
        manualCalories: isNutritionAutoCalculated ? undefined : manualTotals.calories,
        manualProteins: isNutritionAutoCalculated ? undefined : manualTotals.proteins,
        manualFats: isNutritionAutoCalculated ? undefined : manualTotals.fats,
        manualCarbs: isNutritionAutoCalculated ? undefined : manualTotals.carbs,
        manualFiber: isNutritionAutoCalculated ? undefined : manualTotals.fiber,
        manualAlcohol: isNutritionAutoCalculated ? undefined : manualTotals.alcohol,
    };
}

function buildMealManualNutritionPatchValue(meal: Meal): Partial<MealFormValues> {
    return {
        manualCalories: meal.manualCalories ?? meal.totalCalories,
        manualProteins: meal.manualProteins ?? meal.totalProteins,
        manualFats: meal.manualFats ?? meal.totalFats,
        manualCarbs: meal.manualCarbs ?? meal.totalCarbs,
        manualFiber: meal.manualFiber ?? meal.totalFiber,
        manualAlcohol: meal.manualAlcohol ?? meal.totalAlcohol,
    };
}

function toNullable<T>(value: T | null | undefined): T | null {
    return value ?? null;
}
