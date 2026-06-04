import { HttpStatusCode } from '@angular/common/http';

import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { getNumberProperty } from '../../../../../shared/lib/unknown-value.utils';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../../shared/models/ai.data';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { MeasurementUnit } from '../../../models/product.data';
import type { ProductAiRecognitionFormModel, ProductAiRecognitionResult } from '../product-ai-recognition-dialog.types';

export function createProductAiRecognitionFormModel(): ProductAiRecognitionFormModel {
    return {
        name: '',
        portionAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
        baseUnit: MeasurementUnit.G,
        caloriesPerBase: 0,
        proteinsPerBase: 0,
        fatsPerBase: 0,
        carbsPerBase: 0,
        fiberPerBase: 0,
        alcoholPerBase: 0,
    };
}

export type ProductAiRecognitionResultBuildParams = {
    model: ProductAiRecognitionFormModel;
    selection: ImageSelection | null;
    itemNames: readonly string[];
    results: readonly FoodVisionItem[];
    description: string | null;
};

export function buildProductAiRecognitionResult(params: ProductAiRecognitionResultBuildParams): ProductAiRecognitionResult {
    const { model, selection, itemNames, results, description } = params;
    const name = model.name.trim();
    const baseUnit = model.baseUnit;
    const requestedBaseAmount = getNumericValue(model.portionAmount);
    const baseAmount = requestedBaseAmount > 0 ? requestedBaseAmount : getRecognizedAmount(results, baseUnit);

    return {
        name: name.length > 0 ? name : (itemNames[0] ?? ''),
        description,
        image: selection !== null ? { ...selection } : null,
        baseAmount,
        baseUnit,
        caloriesPerBase: getNumericValue(model.caloriesPerBase),
        proteinsPerBase: getNumericValue(model.proteinsPerBase),
        fatsPerBase: getNumericValue(model.fatsPerBase),
        carbsPerBase: getNumericValue(model.carbsPerBase),
        fiberPerBase: getNumericValue(model.fiberPerBase),
        alcoholPerBase: getNumericValue(model.alcoholPerBase),
    };
}

export function buildProductAiRecognitionModelFromNutrition(
    items: readonly FoodVisionItem[],
    nutrition: FoodNutritionResponse,
): ProductAiRecognitionFormModel {
    const primary = items.length > 0 ? items[0] : null;
    const name = primary === null ? '' : capitalizeName(primary.nameLocal?.trim() ?? primary.nameEn.trim());
    const baseUnit = resolveAiMeasurementUnit(primary?.unit);

    return {
        name,
        portionAmount: getRecognizedAmount(items, baseUnit),
        baseUnit,
        caloriesPerBase: nutrition.calories,
        proteinsPerBase: nutrition.protein,
        fatsPerBase: nutrition.fat,
        carbsPerBase: nutrition.carbs,
        fiberPerBase: nutrition.fiber,
        alcoholPerBase: nutrition.alcohol,
    };
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
