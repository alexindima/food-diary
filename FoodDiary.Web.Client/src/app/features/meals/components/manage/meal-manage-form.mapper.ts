import { FormArray, FormControl, FormGroup, type ValidatorFn, Validators } from '@angular/forms';

import { normalizeMealType } from '../../../../shared/lib/meal-type.util';
import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../../shared/lib/satiety-level.utils';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import type { Recipe } from '../../../recipes/models/recipe.data';
import {
    type Consumption,
    type ConsumptionItem,
    type ConsumptionItemManageDto,
    type ConsumptionManageDto,
    ConsumptionSourceType,
} from '../../models/meal.data';
import type {
    ConsumptionFormData,
    ConsumptionFormValues,
    ConsumptionItemFormData,
    ConsumptionItemFormValues,
    NutritionTotals,
} from './meal-manage.types';

export type MealManageFormCallbacks = {
    createItem: () => FormGroup<ConsumptionItemFormData>;
    createItemsValidator: () => ValidatorFn;
};

export type MealManageDtoCallbacks = {
    aiSessions: ConsumptionManageDto['aiSessions'];
    buildDateTime: () => Date;
    convertRecipeGramsToServings: (recipe: Recipe, amount: number) => number;
    manualTotals: NutritionTotals;
};

export type MealManageFormPatchValue = Partial<ConsumptionFormValues>;

export function createMealManageForm(callbacks: MealManageFormCallbacks, now = new Date()): FormGroup<ConsumptionFormData> {
    return new FormGroup<ConsumptionFormData>({
        date: new FormControl<string>(getDateInputValue(now), {
            nonNullable: true,
            validators: Validators.required,
        }),
        time: new FormControl<string>(getTimeInputValue(now), {
            nonNullable: true,
            validators: Validators.required,
        }),
        mealType: new FormControl<string | null>(null),
        items: new FormArray<FormGroup<ConsumptionItemFormData>>([callbacks.createItem()], callbacks.createItemsValidator()),
        comment: new FormControl<string | null>(null),
        imageUrl: new FormControl<ImageSelection | null>(null),
        isNutritionAutoCalculated: new FormControl<boolean>(true, { nonNullable: true }),
        manualCalories: new FormControl<number | null>(null),
        manualProteins: new FormControl<number | null>(null),
        manualFats: new FormControl<number | null>(null),
        manualCarbs: new FormControl<number | null>(null),
        manualFiber: new FormControl<number | null>(null),
        manualAlcohol: new FormControl<number | null>(null, [Validators.min(0)]),
        preMealSatietyLevel: new FormControl<number | null>(DEFAULT_SATIETY_LEVEL),
        postMealSatietyLevel: new FormControl<number | null>(DEFAULT_SATIETY_LEVEL),
    });
}

export function buildMealManageDto(formValue: ConsumptionFormValues, callbacks: MealManageDtoCallbacks): ConsumptionManageDto {
    const image = formValue.imageUrl;
    const isNutritionAutoCalculated = formValue.isNutritionAutoCalculated;

    return {
        date: callbacks.buildDateTime(),
        mealType: formValue.mealType ?? undefined,
        comment: formValue.comment ?? undefined,
        imageUrl: image?.url ?? undefined,
        imageAssetId: image?.assetId ?? undefined,
        items: mapConsumptionItems(formValue.items, callbacks.convertRecipeGramsToServings),
        aiSessions: callbacks.aiSessions,
        isNutritionAutoCalculated,
        ...buildManualNutritionPayload(isNutritionAutoCalculated, callbacks.manualTotals),
        preMealSatietyLevel: normalizeSatietyLevel(formValue.preMealSatietyLevel),
        postMealSatietyLevel: normalizeSatietyLevel(formValue.postMealSatietyLevel),
    };
}

export function buildMealManageFormPatchValue(consumption: Consumption): MealManageFormPatchValue {
    const date = new Date(consumption.date);
    return {
        date: getDateInputValue(date),
        time: getTimeInputValue(date),
        mealType: normalizeMealType(consumption.mealType),
        comment: toNullable(consumption.comment),
        imageUrl: {
            url: toNullable(consumption.imageUrl),
            assetId: toNullable(consumption.imageAssetId),
        },
        isNutritionAutoCalculated: consumption.isNutritionAutoCalculated,
        ...buildConsumptionManualNutritionPatchValue(consumption),
        preMealSatietyLevel: normalizeSatietyLevel(toNullable(consumption.preMealSatietyLevel)),
        postMealSatietyLevel: normalizeSatietyLevel(toNullable(consumption.postMealSatietyLevel)),
    };
}

export function getConsumptionItemInitialAmount(
    item: ConsumptionItem,
    convertRecipeServingsToGrams: (item: ConsumptionItem) => number,
): number {
    return item.sourceType === ConsumptionSourceType.Recipe ? convertRecipeServingsToGrams(item) : item.amount;
}

export function getDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = padNumber(date.getMonth() + 1);
    const day = padNumber(date.getDate());
    return `${year}-${month}-${day}`;
}

export function getTimeInputValue(date: Date): string {
    const hours = padNumber(date.getHours());
    const minutes = padNumber(date.getMinutes());
    return `${hours}:${minutes}`;
}

function mapConsumptionItems(
    items: ConsumptionItemFormValues[],
    convertRecipeGramsToServings: (recipe: Recipe, amount: number) => number,
): ConsumptionItemManageDto[] {
    return items.flatMap(item => mapConsumptionItem(item, convertRecipeGramsToServings));
}

function mapConsumptionItem(
    item: ConsumptionItemFormValues,
    convertRecipeGramsToServings: (recipe: Recipe, amount: number) => number,
): ConsumptionItemManageDto[] {
    const amount = normalizeItemAmount(item.amount);
    const sourceType = item.sourceType;

    if (sourceType === ConsumptionSourceType.Product && item.product !== null) {
        return [{ productId: item.product.id, recipeId: null, amount }];
    }

    if (sourceType === ConsumptionSourceType.Recipe && item.recipe !== null) {
        return [
            {
                recipeId: item.recipe.id,
                productId: null,
                amount: convertRecipeGramsToServings(item.recipe, amount),
            },
        ];
    }

    return [];
}

function normalizeItemAmount(value: unknown): number {
    const parsedAmount = Number(value);
    return Number.isNaN(parsedAmount) || parsedAmount === 0 ? 0 : parsedAmount;
}

function buildManualNutritionPayload(isNutritionAutoCalculated: boolean, manualTotals: NutritionTotals): Partial<ConsumptionManageDto> {
    return {
        manualCalories: isNutritionAutoCalculated ? undefined : manualTotals.calories,
        manualProteins: isNutritionAutoCalculated ? undefined : manualTotals.proteins,
        manualFats: isNutritionAutoCalculated ? undefined : manualTotals.fats,
        manualCarbs: isNutritionAutoCalculated ? undefined : manualTotals.carbs,
        manualFiber: isNutritionAutoCalculated ? undefined : manualTotals.fiber,
        manualAlcohol: isNutritionAutoCalculated ? undefined : manualTotals.alcohol,
    };
}

function buildConsumptionManualNutritionPatchValue(consumption: Consumption): Partial<ConsumptionFormValues> {
    return {
        manualCalories: consumption.manualCalories ?? consumption.totalCalories,
        manualProteins: consumption.manualProteins ?? consumption.totalProteins,
        manualFats: consumption.manualFats ?? consumption.totalFats,
        manualCarbs: consumption.manualCarbs ?? consumption.totalCarbs,
        manualFiber: consumption.manualFiber ?? consumption.totalFiber,
        manualAlcohol: consumption.manualAlcohol ?? consumption.totalAlcohol,
    };
}

function toNullable<T>(value: T | null | undefined): T | null {
    return value ?? null;
}

function padNumber(value: number): string {
    return value.toString().padStart(2, '0');
}
