import { HttpStatusCode } from '@angular/common/http';
import { FormControl, FormGroup } from '@angular/forms';

import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { getNumberProperty } from '../../../../../shared/lib/unknown-value.utils';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../../shared/models/ai.data';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { MeasurementUnit } from '../../../models/product.data';
import type { ProductAiRecognitionFormGroup, ProductAiRecognitionResult } from '../product-ai-recognition-dialog.types';

export function createProductAiRecognitionForm(): ProductAiRecognitionFormGroup {
    return new FormGroup({
        name: new FormControl('', { nonNullable: true }),
        portionAmount: new FormControl(DEFAULT_NUTRITION_BASE_AMOUNT, { nonNullable: true }),
        baseUnit: new FormControl<MeasurementUnit>(MeasurementUnit.G, { nonNullable: true }),
        caloriesPerBase: new FormControl(0, { nonNullable: true }),
        proteinsPerBase: new FormControl(0, { nonNullable: true }),
        fatsPerBase: new FormControl(0, { nonNullable: true }),
        carbsPerBase: new FormControl(0, { nonNullable: true }),
        fiberPerBase: new FormControl(0, { nonNullable: true }),
        alcoholPerBase: new FormControl(0, { nonNullable: true }),
    });
}

export type ProductAiRecognitionResultBuildParams = {
    form: ProductAiRecognitionFormGroup;
    selection: ImageSelection | null;
    itemNames: readonly string[];
    results: readonly FoodVisionItem[];
    description: string | null;
};

export function buildProductAiRecognitionResult(params: ProductAiRecognitionResultBuildParams): ProductAiRecognitionResult {
    const { form, selection, itemNames, results, description } = params;
    const name = form.controls.name.value.trim();
    const baseUnit = form.controls.baseUnit.value;
    const requestedBaseAmount = getNumericValue(form.controls.portionAmount.value);
    const baseAmount = requestedBaseAmount > 0 ? requestedBaseAmount : getRecognizedAmount(results, baseUnit);

    return {
        name: name.length > 0 ? name : (itemNames[0] ?? ''),
        description,
        image: selection !== null ? { ...selection } : null,
        baseAmount,
        baseUnit,
        caloriesPerBase: getNumericValue(form.controls.caloriesPerBase.value),
        proteinsPerBase: getNumericValue(form.controls.proteinsPerBase.value),
        fatsPerBase: getNumericValue(form.controls.fatsPerBase.value),
        carbsPerBase: getNumericValue(form.controls.carbsPerBase.value),
        fiberPerBase: getNumericValue(form.controls.fiberPerBase.value),
        alcoholPerBase: getNumericValue(form.controls.alcoholPerBase.value),
    };
}

export function applyNutritionToProductAiRecognitionForm(
    form: ProductAiRecognitionFormGroup,
    items: readonly FoodVisionItem[],
    nutrition: FoodNutritionResponse,
): void {
    const primary = items.length > 0 ? items[0] : null;
    const name = primary === null ? '' : capitalizeName(primary.nameLocal?.trim() ?? primary.nameEn.trim());
    const baseUnit = resolveAiMeasurementUnit(primary?.unit);
    form.patchValue({
        name,
        portionAmount: getRecognizedAmount(items, baseUnit),
        baseUnit,
        caloriesPerBase: nutrition.calories,
        proteinsPerBase: nutrition.protein,
        fatsPerBase: nutrition.fat,
        carbsPerBase: nutrition.carbs,
        fiberPerBase: nutrition.fiber,
        alcoholPerBase: nutrition.alcohol,
    });
}

export function normalizeItemsForNutrition(items: readonly FoodVisionItem[]): FoodVisionItem[] {
    return items.map(item => {
        const baseUnit = resolveAiMeasurementUnit(item.unit);
        const normalizedUnit = baseUnit === MeasurementUnit.PCS ? 'pcs' : baseUnit.toLowerCase();
        const amount = getNumericValue(item.amount);
        const normalizedAmount = amount > 0 ? amount : getDefaultBaseAmount(baseUnit);

        return {
            ...item,
            amount: normalizedAmount,
            unit: normalizedUnit,
        };
    });
}

export function resolveAiMeasurementUnit(unit?: string | null): MeasurementUnit {
    if (unit === null || unit === undefined || unit.length === 0) {
        return MeasurementUnit.G;
    }

    const normalized = unit.trim().toLowerCase();
    if (['g', 'gram', 'grams', 'gr'].includes(normalized)) {
        return MeasurementUnit.G;
    }
    if (['ml', 'l', 'liter', 'liters'].includes(normalized)) {
        return MeasurementUnit.ML;
    }
    if (['pcs', 'pc', 'piece', 'pieces'].includes(normalized)) {
        return MeasurementUnit.PCS;
    }
    return MeasurementUnit.G;
}

export function getRecognizedAmount(items: readonly FoodVisionItem[], unit: MeasurementUnit): number {
    const compatibleAmounts = items
        .filter(item => resolveAiMeasurementUnit(item.unit) === unit)
        .map(item => getNumericValue(item.amount))
        .filter(amount => amount > 0);

    if (compatibleAmounts.length > 0) {
        return compatibleAmounts.reduce((total, amount) => total + amount, 0);
    }

    return getDefaultBaseAmount(unit);
}

export function mapAiRecognitionErrorKey(error: unknown): string {
    const status = getNumberProperty(error, 'status');
    if (status === HttpStatusCode.Forbidden) {
        return 'PRODUCT_AI_DIALOG.ERROR_PREMIUM';
    }
    if (status === HttpStatusCode.TooManyRequests) {
        return 'PRODUCT_AI_DIALOG.ERROR_QUOTA';
    }
    return 'PRODUCT_AI_DIALOG.ERROR_GENERIC';
}

export function mapAiNutritionErrorKey(error: unknown): string {
    const status = getNumberProperty(error, 'status');
    return status === HttpStatusCode.TooManyRequests ? 'PRODUCT_AI_DIALOG.ERROR_QUOTA' : 'PRODUCT_AI_DIALOG.NUTRITION_ERROR';
}

export function capitalizeName(value: string): string {
    if (value.length === 0) {
        return '';
    }
    return value.charAt(0).toUpperCase() + value.slice(1);
}

function getDefaultBaseAmount(unit: MeasurementUnit): number {
    return unit === MeasurementUnit.PCS ? 1 : DEFAULT_NUTRITION_BASE_AMOUNT;
}

function getNumericValue(value: number | string): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
}
