import { FormArray, FormControl, FormGroup, type ValidatorFn, Validators } from '@angular/forms';

import { normalizeMealType } from '../../../../../shared/lib/meal-type.util';
import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../../../shared/lib/satiety-level.utils';
import { isRecord } from '../../../../../shared/lib/unknown-value.utils';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import type { Product } from '../../../../products/models/product.data';
import type { Recipe } from '../../../../recipes/models/recipe.data';
import { MEAL_MANAGE_MIN_ITEM_AMOUNT, MEAL_MANAGE_MIN_NUTRITION_VALUE } from '../../../lib/manage/meal-manage.config';
import { getDateInputValue, getTimeInputValue } from '../../../lib/meal-date-input.utils';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    type ConsumptionItem,
    type ConsumptionItemManageDto,
    type ConsumptionManageDto,
    ConsumptionSourceType,
} from '../../../models/meal.data';
import type {
    ConsumptionFormData,
    ConsumptionFormValues,
    ConsumptionItemFormData,
    ConsumptionItemFormValues,
    NutritionTotals,
} from './meal-manage.types';

export type MealManageFormCallbacks = {
    createItem: () => FormGroup<ConsumptionItemFormData>;
    createItemsRule: () => ValidatorFn;
};

export type MealManageDtoCallbacks = {
    aiSessions: ConsumptionManageDto['aiSessions'];
    buildDateTime: () => Date;
    convertRecipeGramsToServings: (recipe: Recipe, amount: number) => number;
    manualTotals: NutritionTotals;
};

export type MealManageFormPatchValue = Partial<ConsumptionFormValues>;

export function createConsumptionItemGroup(
    product: Product | null = null,
    recipe: Recipe | null = null,
    amount: number | null = null,
    sourceType: ConsumptionSourceType = ConsumptionSourceType.Product,
): FormGroup<ConsumptionItemFormData> {
    const group = new FormGroup<ConsumptionItemFormData>({
        sourceType: new FormControl<ConsumptionSourceType>(sourceType, { nonNullable: true }),
        product: new FormControl<Product | null>(product),
        recipe: new FormControl<Recipe | null>(recipe),
        amount: new FormControl<number | null>(amount),
    });

    updateConsumptionItemAmountControlState(group);
    return group;
}

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
        items: new FormArray<FormGroup<ConsumptionItemFormData>>([callbacks.createItem()], callbacks.createItemsRule()),
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

export function createMealManageFormValue(now = new Date()): ConsumptionFormValues {
    return {
        date: getDateInputValue(now),
        time: getTimeInputValue(now),
        mealType: null,
        items: [createConsumptionItemValue()],
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

export function createConsumptionItemValue(
    product: ConsumptionItemFormValues['product'] = null,
    recipe: ConsumptionItemFormValues['recipe'] = null,
    amount: number | null = null,
    sourceType: ConsumptionSourceType = ConsumptionSourceType.Product,
): ConsumptionItemFormValues {
    return {
        sourceType,
        product,
        recipe,
        amount,
    };
}

export function createMealConsumptionItemsRule(getAiSessions: () => ConsumptionAiSessionManageDto[]): ValidatorFn {
    return control => {
        const value: unknown = control.value;
        const hasManualItems = Array.isArray(value) ? value.some(hasSelectedSource) : false;
        const hasAiItems = getAiSessions().length > 0;
        return hasManualItems || hasAiItems ? null : { nonEmptyArray: true };
    };
}

export function applyMealConsumptionItemRules(items: FormArray<FormGroup<ConsumptionItemFormData>>): void {
    items.controls.forEach(group => {
        const isEmpty = isConsumptionItemEmpty(group);
        if (isEmpty) {
            group.controls.product.clearValidators();
            group.controls.recipe.clearValidators();
            group.controls.amount.clearValidators();
        } else {
            const sourceType = group.controls.sourceType.value;
            if (sourceType === ConsumptionSourceType.Product) {
                group.controls.product.setValidators([Validators.required]);
                group.controls.recipe.clearValidators();
            } else {
                group.controls.recipe.setValidators([Validators.required]);
                group.controls.product.clearValidators();
            }
            group.controls.amount.setValidators([Validators.required, Validators.min(MEAL_MANAGE_MIN_ITEM_AMOUNT)]);
        }

        group.controls.product.updateValueAndValidity({ emitEvent: false });
        group.controls.recipe.updateValueAndValidity({ emitEvent: false });
        group.controls.amount.updateValueAndValidity({ emitEvent: false });
    });

    items.updateValueAndValidity({ emitEvent: false });
}

export function applyMealManualNutritionRules(form: FormGroup<ConsumptionFormData>, isAuto: boolean): void {
    const caloriesValidators = isAuto
        ? [Validators.min(MEAL_MANAGE_MIN_NUTRITION_VALUE)]
        : [Validators.required, Validators.min(MEAL_MANAGE_MIN_NUTRITION_VALUE)];
    form.controls.manualCalories.setValidators(caloriesValidators);
    form.controls.manualCalories.updateValueAndValidity({ emitEvent: false });

    getOptionalManualNutritionControls(form).forEach(control => {
        control.setValidators([Validators.min(MEAL_MANAGE_MIN_NUTRITION_VALUE)]);
        control.updateValueAndValidity({ emitEvent: false });
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

export { getDateInputValue, getTimeInputValue } from '../../../lib/meal-date-input.utils';

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

function updateConsumptionItemAmountControlState(group: FormGroup<ConsumptionItemFormData>): void {
    const shouldDisable = group.controls.product.value === null && group.controls.recipe.value === null;
    if (shouldDisable && group.controls.amount.enabled) {
        group.controls.amount.disable({ emitEvent: false });
        return;
    }

    if (!shouldDisable && group.controls.amount.disabled) {
        group.controls.amount.enable({ emitEvent: false });
    }
}

function hasSelectedSource(value: unknown): boolean {
    return (
        isRecord(value) &&
        ((value['product'] !== null && value['product'] !== undefined) || (value['recipe'] !== null && value['recipe'] !== undefined))
    );
}

function isConsumptionItemEmpty(group: FormGroup<ConsumptionItemFormData>): boolean {
    const hasSource = group.controls.product.value !== null || group.controls.recipe.value !== null;
    const amount = group.controls.amount.value ?? 0;
    return !hasSource && amount <= 0;
}

function getOptionalManualNutritionControls(form: FormGroup<ConsumptionFormData>): Array<FormControl<number | null>> {
    return [
        form.controls.manualProteins,
        form.controls.manualFats,
        form.controls.manualCarbs,
        form.controls.manualFiber,
        form.controls.manualAlcohol,
    ];
}
